using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SkipDrama_YuanShen
{
    internal sealed class SdlGamepadSnapshot
    {
        internal bool A { get; set; }
        internal bool B { get; set; }
        internal bool X { get; set; }
        internal bool Y { get; set; }
        internal bool Guide { get; set; }
        internal bool Share { get; set; }
        internal bool DpadLeft { get; set; }
        internal bool DpadRight { get; set; }
        internal bool DpadUp { get; set; }
        internal bool DpadDown { get; set; }
        internal bool LeftShoulder { get; set; }
        internal bool RightShoulder { get; set; }
        internal short LeftTrigger { get; set; }
        internal short RightTrigger { get; set; }
        internal short LeftX { get; set; }
        internal short LeftY { get; set; }
        internal short RightX { get; set; }
        internal short RightY { get; set; }
    }

    internal sealed class SdlGamepadInputService : IDisposable
    {
        private const ushort MicrosoftVendorId = 0x045E;
        private const int PollIntervalMilliseconds = 5;

        private readonly CancellationTokenSource _stop = new CancellationTokenSource();
        private readonly object _deviceFilterGate = new object();
        private readonly HashSet<uint> _excludedOutputIds = new HashSet<uint>();
        private Task _worker;
        private IntPtr _gamepad;
        private bool _disposed;
        private string _lastDeviceStatus;
        private string _preferredPath;
        private HashSet<uint> _idsBeforeVirtualOutput;
        private volatile bool _virtualOutputRegistrationInProgress;

        internal event Action<SdlGamepadSnapshot> SnapshotReceived;
        internal event Action<string> DeviceChanged;
        internal event Action<Exception> Error;

        internal Task StartAsync()
        {
            if (_worker != null)
            {
                return Task.CompletedTask;
            }

            var started = new TaskCompletionSource<bool>();
            _worker = Task.Run(() => Run(started, _stop.Token));
            return started.Task;
        }

        private void Run(TaskCompletionSource<bool> started, CancellationToken token)
        {
            try
            {
                Sdl3Native.SDL_SetHint(Sdl3Native.BackgroundEventsHint, "1");
                if (!Sdl3Native.SDL_Init(Sdl3Native.InitGamepad))
                {
                    throw new InvalidOperationException("SDL3 初始化失败: " + Sdl3Native.GetError());
                }

                TryOpenInputController();
                started.TrySetResult(true);

                while (!token.IsCancellationRequested)
                {
                    if (_gamepad == IntPtr.Zero || !Sdl3Native.SDL_GamepadConnected(_gamepad))
                    {
                        CloseCurrentController();
                        if (!_virtualOutputRegistrationInProgress)
                        {
                            TryOpenInputController();
                        }
                        token.WaitHandle.WaitOne(250);
                        continue;
                    }

                    Sdl3Native.SDL_UpdateGamepads();
                    SnapshotReceived?.Invoke(ReadSnapshot());
                    token.WaitHandle.WaitOne(PollIntervalMilliseconds);
                }
            }
            catch (Exception ex)
            {
                started.TrySetException(ex);
                Error?.Invoke(ex);
            }
            finally
            {
                CloseCurrentController();
                Sdl3Native.SDL_Quit();
            }
        }

        private void TryOpenInputController()
        {
            var ids = GetGamepadIds();
            var candidates = ids
                .Select(id => new ControllerCandidate(id))
                .Where(candidate => !IsExcludedOutput(candidate.Id))
                .OrderByDescending(candidate => candidate.MatchesPath(_preferredPath))
                .ThenByDescending(candidate => candidate.Vendor == MicrosoftVendorId)
                .ThenBy(candidate => candidate.Id)
                .ToList();

            foreach (var candidate in candidates)
            {
                if (candidate.Vendor == MicrosoftVendorId)
                {
                    ApplyXboxSeriesMapping(candidate.Id);
                }
                var gamepad = Sdl3Native.SDL_OpenGamepad(candidate.Id);
                if (gamepad == IntPtr.Zero)
                {
                    continue;
                }

                _gamepad = gamepad;
                if (string.IsNullOrEmpty(_preferredPath) && !string.IsNullOrEmpty(candidate.Path))
                {
                    _preferredPath = candidate.Path;
                }
                NotifyDeviceChanged($"{candidate.Name} (VID {candidate.Vendor:X4}, PID {candidate.Product:X4})");
                return;
            }

            NotifyDeviceChanged("未检测到真实手柄");
        }

        internal void BeginVirtualOutputRegistration()
        {
            _virtualOutputRegistrationInProgress = true;
            _idsBeforeVirtualOutput = new HashSet<uint>(GetGamepadIds());
        }

        internal async Task CompleteVirtualOutputRegistrationAsync()
        {
            try
            {
                // SimWinGamePad normally appears synchronously, but sample a few times for slower driver startup.
                for (var attempt = 0; attempt < 4; attempt++)
                {
                    await Task.Delay(150).ConfigureAwait(false);
                    var currentIds = GetGamepadIds();
                    lock (_deviceFilterGate)
                    {
                        foreach (var id in currentIds)
                        {
                            if (_idsBeforeVirtualOutput == null || !_idsBeforeVirtualOutput.Contains(id))
                            {
                                _excludedOutputIds.Add(id);
                            }
                        }
                    }
                }
            }
            finally
            {
                _virtualOutputRegistrationInProgress = false;
            }
        }

        internal void CancelVirtualOutputRegistration()
        {
            _virtualOutputRegistrationInProgress = false;
        }

        private bool IsExcludedOutput(uint instanceId)
        {
            lock (_deviceFilterGate)
            {
                return _excludedOutputIds.Contains(instanceId);
            }
        }

        private static List<uint> GetGamepadIds()
        {
            int count;
            var pointer = Sdl3Native.SDL_GetGamepads(out count);
            var result = new List<uint>(Math.Max(count, 0));

            try
            {
                for (var index = 0; index < count; index++)
                {
                    result.Add(unchecked((uint)System.Runtime.InteropServices.Marshal.ReadInt32(pointer, index * sizeof(uint))));
                }
            }
            finally
            {
                if (pointer != IntPtr.Zero)
                {
                    Sdl3Native.SDL_free(pointer);
                }
            }

            return result;
        }

        private static void ApplyXboxSeriesMapping(uint instanceId)
        {
            var guidBuffer = new byte[33];
            Sdl3Native.SDL_GUIDToString(Sdl3Native.SDL_GetJoystickGUIDForID(instanceId), guidBuffer, guidBuffer.Length);
            var guid = Encoding.ASCII.GetString(guidBuffer).TrimEnd('\0');
            if (string.IsNullOrEmpty(guid))
            {
                return;
            }

            var mapping = guid + ",Xbox Controller,platform:Windows," +
                "a:b0,b:b1,x:b2,y:b3,back:b6,guide:b10,start:b7," +
                "leftstick:b8,rightstick:b9,leftshoulder:b4,rightshoulder:b5," +
                "dpup:h0.1,dpdown:h0.4,dpleft:h0.8,dpright:h0.2," +
                "leftx:a0,lefty:a1,rightx:a2,righty:a3,lefttrigger:a4,righttrigger:a5,misc1:b11,";
            Sdl3Native.SDL_AddGamepadMapping(mapping);
        }

        private SdlGamepadSnapshot ReadSnapshot()
        {
            return new SdlGamepadSnapshot
            {
                A = Button(SdlGamepadButton.South),
                B = Button(SdlGamepadButton.East),
                X = Button(SdlGamepadButton.West),
                Y = Button(SdlGamepadButton.North),
                Guide = Button(SdlGamepadButton.Guide),
                Share = Button(SdlGamepadButton.Misc1),
                DpadLeft = Button(SdlGamepadButton.DpadLeft),
                DpadRight = Button(SdlGamepadButton.DpadRight),
                DpadUp = Button(SdlGamepadButton.DpadUp),
                DpadDown = Button(SdlGamepadButton.DpadDown),
                LeftShoulder = Button(SdlGamepadButton.LeftShoulder),
                RightShoulder = Button(SdlGamepadButton.RightShoulder),
                LeftTrigger = Axis(SdlGamepadAxis.LeftTrigger),
                RightTrigger = Axis(SdlGamepadAxis.RightTrigger),
                LeftX = Axis(SdlGamepadAxis.LeftX),
                LeftY = Axis(SdlGamepadAxis.LeftY),
                RightX = Axis(SdlGamepadAxis.RightX),
                RightY = Axis(SdlGamepadAxis.RightY)
            };
        }

        private bool Button(SdlGamepadButton button)
        {
            return Sdl3Native.SDL_GetGamepadButton(_gamepad, button);
        }

        private short Axis(SdlGamepadAxis axis)
        {
            return Sdl3Native.SDL_GetGamepadAxis(_gamepad, axis);
        }

        private void CloseCurrentController()
        {
            if (_gamepad != IntPtr.Zero)
            {
                Sdl3Native.SDL_CloseGamepad(_gamepad);
                _gamepad = IntPtr.Zero;
                NotifyDeviceChanged("手柄已断开");
            }
        }

        private void NotifyDeviceChanged(string status)
        {
            if (string.Equals(_lastDeviceStatus, status, StringComparison.Ordinal))
            {
                return;
            }

            _lastDeviceStatus = status;
            DeviceChanged?.Invoke(status);
        }

        internal async Task StopAsync()
        {
            _stop.Cancel();
            if (_worker != null)
            {
                await _worker.ConfigureAwait(false);
            }
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            _stop.Cancel();
            _stop.Dispose();
        }

        private sealed class ControllerCandidate
        {
            internal ControllerCandidate(uint id)
            {
                Id = id;
                Name = Sdl3Native.StringFromUtf8(Sdl3Native.SDL_GetGamepadNameForID(id));
                Path = Sdl3Native.StringFromUtf8(Sdl3Native.SDL_GetGamepadPathForID(id));
                Vendor = Sdl3Native.SDL_GetGamepadVendorForID(id);
                Product = Sdl3Native.SDL_GetGamepadProductForID(id);
            }

            internal uint Id { get; }
            internal string Name { get; }
            internal string Path { get; }
            internal ushort Vendor { get; }
            internal ushort Product { get; }

            internal bool MatchesPath(string preferredPath)
            {
                return !string.IsNullOrEmpty(preferredPath) &&
                       string.Equals(Path, preferredPath, StringComparison.OrdinalIgnoreCase);
            }

        }
    }
}
