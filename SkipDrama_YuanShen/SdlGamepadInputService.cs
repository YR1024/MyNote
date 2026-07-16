using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
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
        internal bool Start { get; set; }
        internal bool Back { get; set; }
        internal bool LeftStickClick { get; set; }
        internal bool RightStickClick { get; set; }
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
        internal short LeftTriggerMapped { get; set; }
        internal short RightTriggerMapped { get; set; }
        internal bool ShareMapped { get; set; }
        internal bool ShareRaw { get; set; }
        internal string RawAxes { get; set; }
        internal string RawButtons { get; set; }
    }

    internal sealed class SdlButtonChangedEvent
    {
        internal SdlButtonChangedEvent(bool share, bool guide, bool pressed, string source, byte button, uint deviceId)
        {
            Share = share;
            Guide = guide;
            Pressed = pressed;
            Source = source;
            Button = button;
            DeviceId = deviceId;
        }

        internal bool Share { get; }
        internal bool Guide { get; }
        internal bool Pressed { get; }
        internal string Source { get; }
        internal byte Button { get; }
        internal uint DeviceId { get; }
    }

    internal sealed class SdlGamepadInputService : IDisposable
    {
        private const ushort MicrosoftVendorId = 0x045E;
        private const int PollIntervalMilliseconds = 5;
        private const int XboxShareRawButton = 11;
        private const int XboxGuideRawButton = 10;

        private readonly CancellationTokenSource _stop = new CancellationTokenSource();
        private readonly object _deviceFilterGate = new object();
        private readonly HashSet<uint> _excludedOutputIds = new HashSet<uint>();
        private Task _worker;
        private IntPtr _gamepad;
        private bool _disposed;
        private string _lastDeviceStatus;
        private string _preferredPath;
        private uint _activeInstanceId;
        private HashSet<uint> _idsBeforeVirtualOutput;
        private volatile bool _virtualOutputRegistrationInProgress;

        internal event Action<SdlGamepadSnapshot> SnapshotReceived;
        internal event Action<SdlButtonChangedEvent> ButtonChanged;
        internal event Action<string> EventTrace;
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
                    PumpWindowsMessages();
                    PollEvents();

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
                var gamepad = Sdl3Native.SDL_OpenGamepad(candidate.Id);
                if (gamepad == IntPtr.Zero)
                {
                    continue;
                }

                _gamepad = gamepad;
                _activeInstanceId = candidate.Id;
                if (string.IsNullOrEmpty(_preferredPath) && !string.IsNullOrEmpty(candidate.Path))
                {
                    _preferredPath = candidate.Path;
                }
                NotifyDeviceChanged($"{candidate.Name} (VID {candidate.Vendor:X4}, PID {candidate.Product:X4})");
                return;
            }

            NotifyDeviceChanged("未检测到真实手柄");
        }

        private void PollEvents()
        {
            while (Sdl3Native.SDL_PollEvent(out var ev))
            {
                if (_activeInstanceId != 0 && ev.Which != 0 && ev.Which != _activeInstanceId)
                {
                    continue;
                }

                switch (ev.Type)
                {
                    case Sdl3Native.EventGamepadButtonDown:
                    case Sdl3Native.EventGamepadButtonUp:
                        OnGamepadButtonEvent(ev);
                        break;
                    case Sdl3Native.EventJoystickButtonDown:
                    case Sdl3Native.EventJoystickButtonUp:
                        OnJoystickButtonEvent(ev);
                        break;
                }
            }
        }

        private void OnGamepadButtonEvent(SdlEvent ev)
        {
            var share = ev.Button == (byte)SdlGamepadButton.Misc1;
            var guide = ev.Button == (byte)SdlGamepadButton.Guide;
            if (!share && !guide)
            {
                TraceButtonEvent("SDL Event Gamepad", ev);
                return;
            }

            ButtonChanged?.Invoke(new SdlButtonChangedEvent(
                share,
                guide,
                ev.Type == Sdl3Native.EventGamepadButtonDown || ev.Down != 0,
                "SDL Event Gamepad",
                ev.Button,
                ev.Which));
        }

        private void OnJoystickButtonEvent(SdlEvent ev)
        {
            var share = ev.Button == XboxShareRawButton;
            var guide = ev.Button == XboxGuideRawButton;
            if (!share && !guide)
            {
                TraceButtonEvent("SDL Event Joystick", ev);
                return;
            }

            ButtonChanged?.Invoke(new SdlButtonChangedEvent(
                share,
                guide,
                ev.Type == Sdl3Native.EventJoystickButtonDown || ev.Down != 0,
                "SDL Event Joystick",
                ev.Button,
                ev.Which));
        }

        private void TraceButtonEvent(string source, SdlEvent ev)
        {
            EventTrace?.Invoke(source + ": type=0x" + ev.Type.ToString("X") + ", button=" + ev.Button + ", down=" + (ev.Type == Sdl3Native.EventGamepadButtonDown || ev.Type == Sdl3Native.EventJoystickButtonDown || ev.Down != 0) + ", device=" + ev.Which);
        }

        private static void PumpWindowsMessages()
        {
            while (PeekMessage(out var message, IntPtr.Zero, 0, 0, RemoveMessage))
            {
                TranslateMessage(ref message);
                DispatchMessage(ref message);
            }
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
            Sdl3Native.SDL_PumpEvents();

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

        private SdlGamepadSnapshot ReadSnapshot()
        {
            var shareMapped = Button(SdlGamepadButton.Misc1);
            var shareRaw = RawJoystickButton(XboxShareRawButton);
            var leftTrigger = TriggerAxis(SdlGamepadAxis.LeftTrigger, out var leftTriggerMapped);
            var rightTrigger = TriggerAxis(SdlGamepadAxis.RightTrigger, out var rightTriggerMapped);

            return new SdlGamepadSnapshot
            {
                A = Button(SdlGamepadButton.South),
                B = Button(SdlGamepadButton.East),
                X = Button(SdlGamepadButton.West),
                Y = Button(SdlGamepadButton.North),
                Start = Button(SdlGamepadButton.Start),
                Back = Button(SdlGamepadButton.Back),
                LeftStickClick = Button(SdlGamepadButton.LeftStick),
                RightStickClick = Button(SdlGamepadButton.RightStick),
                Guide = Button(SdlGamepadButton.Guide),
                Share = shareMapped || shareRaw,
                DpadLeft = Button(SdlGamepadButton.DpadLeft),
                DpadRight = Button(SdlGamepadButton.DpadRight),
                DpadUp = Button(SdlGamepadButton.DpadUp),
                DpadDown = Button(SdlGamepadButton.DpadDown),
                LeftShoulder = Button(SdlGamepadButton.LeftShoulder),
                RightShoulder = Button(SdlGamepadButton.RightShoulder),
                LeftTrigger = leftTrigger,
                RightTrigger = rightTrigger,
                LeftX = Axis(SdlGamepadAxis.LeftX),
                LeftY = Axis(SdlGamepadAxis.LeftY),
                RightX = Axis(SdlGamepadAxis.RightX),
                RightY = Axis(SdlGamepadAxis.RightY),
                LeftTriggerMapped = leftTriggerMapped,
                RightTriggerMapped = rightTriggerMapped,
                ShareMapped = shareMapped,
                ShareRaw = shareRaw,
                RawAxes = RawJoystickAxes(),
                RawButtons = RawJoystickButtons()
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

        private short TriggerAxis(SdlGamepadAxis axis, out short mappedValue)
        {
            mappedValue = Math.Max((short)0, Axis(axis));
            return mappedValue;
        }

        private string RawJoystickAxes()
        {
            var joystick = Sdl3Native.SDL_GetGamepadJoystick(_gamepad);
            if (joystick == IntPtr.Zero)
            {
                return string.Empty;
            }

            var count = Sdl3Native.SDL_GetNumJoystickAxes(joystick);
            var axes = new List<string>(Math.Max(count, 0));
            for (var index = 0; index < count; index++)
            {
                axes.Add(index + ":" + Sdl3Native.SDL_GetJoystickAxis(joystick, index));
            }

            return string.Join(", ", axes);
        }

        private string RawJoystickButtons()
        {
            var joystick = Sdl3Native.SDL_GetGamepadJoystick(_gamepad);
            if (joystick == IntPtr.Zero)
            {
                return string.Empty;
            }

            var count = Sdl3Native.SDL_GetNumJoystickButtons(joystick);
            var pressed = new List<string>();
            for (var index = 0; index < count; index++)
            {
                if (Sdl3Native.SDL_GetJoystickButton(joystick, index))
                {
                    pressed.Add(index.ToString());
                }
            }

            return pressed.Count == 0 ? "-" : string.Join(", ", pressed);
        }

        private bool RawJoystickButton(int button)
        {
            var joystick = Sdl3Native.SDL_GetGamepadJoystick(_gamepad);
            if (joystick == IntPtr.Zero || Sdl3Native.SDL_GetNumJoystickButtons(joystick) <= button)
            {
                return false;
            }

            return Sdl3Native.SDL_GetJoystickButton(joystick, button);
        }

        private void CloseCurrentController()
        {
            if (_gamepad != IntPtr.Zero)
            {
                Sdl3Native.SDL_CloseGamepad(_gamepad);
                _gamepad = IntPtr.Zero;
                _activeInstanceId = 0;
                SnapshotReceived?.Invoke(new SdlGamepadSnapshot());
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

        private const uint RemoveMessage = 0x0001;

        [StructLayout(LayoutKind.Sequential)]
        private struct NativeMessage
        {
            internal IntPtr HWnd;
            internal uint Message;
            internal UIntPtr WParam;
            internal IntPtr LParam;
            internal uint Time;
            internal int PointX;
            internal int PointY;
            internal uint Private;
        }

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool PeekMessage(out NativeMessage message, IntPtr hwnd, uint messageFilterMin, uint messageFilterMax, uint removeMessage);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool TranslateMessage(ref NativeMessage message);

        [DllImport("user32.dll")]
        private static extern IntPtr DispatchMessage(ref NativeMessage message);
    }
}
