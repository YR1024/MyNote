using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace SkipDrama_YuanShen
{
    internal sealed class GameInputSystemButtonListener : IDisposable
    {
        private const int GameInputSystemButtonGuide = 0x00000001;
        private const int GameInputSystemButtonShare = 0x00000002;
        private const int GameInputEnableBackgroundInput = 0x00000040;
        private const int GameInputEnableBackgroundGuideButton = 0x00000080;
        private const int GameInputEnableBackgroundShareButton = 0x00000100;

        private static readonly Guid IidGameInput = new Guid("20EFC1C7-5D9A-43BA-B26F-B807FA48609C");

        private readonly CancellationTokenSource _stop = new CancellationTokenSource();
        private IntPtr _gameInputLibrary;
        private IntPtr _gameInput;
        private IntPtr _dispatcher;
        private ulong _callbackToken;
        private Task _dispatchTask;
        private SystemButtonCallback _callback;
        private bool _started;
        private bool _disposed;

        internal event Action<int, bool> SystemButtonChanged;
        internal event Action<string> StatusChanged;
        internal event Action<Exception> Error;

        internal void Start()
        {
            if (_started)
            {
                return;
            }

            _started = true;

            var initialize = LoadGameInputInitialize();
            var iid = IidGameInput;
            var hr = initialize(ref iid, out _gameInput);
            ThrowIfFailed(hr, "GameInputInitialize failed");
            StatusChanged?.Invoke("GameInput: 已加载 GameInputRedist v3");

            SetFocusPolicy(_gameInput, GameInputEnableBackgroundInput | GameInputEnableBackgroundGuideButton | GameInputEnableBackgroundShareButton);
            StatusChanged?.Invoke("GameInput: 已启用后台 Share/Guide");

            var dispatcherHr = CreateDispatcher(_gameInput, out _dispatcher);
            if (dispatcherHr >= 0 && _dispatcher != IntPtr.Zero)
            {
                _dispatchTask = Task.Run(() => DispatchLoop(_stop.Token));
                StatusChanged?.Invoke("GameInput: dispatcher 已启动");
            }
            else
            {
                StatusChanged?.Invoke("GameInput: dispatcher 不可用，HRESULT=0x" + unchecked((uint)dispatcherHr).ToString("X8"));
            }

            _callback = OnSystemButton;
            hr = RegisterSystemButtonCallback(
                _gameInput,
                IntPtr.Zero,
                GameInputSystemButtonGuide | GameInputSystemButtonShare,
                IntPtr.Zero,
                _callback,
                out _callbackToken);
            ThrowIfFailed(hr, "RegisterSystemButtonCallback failed");

            StatusChanged?.Invoke("GameInput: system button callback 已注册");
        }

        private void DispatchLoop(CancellationToken token)
        {
            try
            {
                while (!token.IsCancellationRequested)
                {
                    Dispatch(_dispatcher, 1000);
                    token.WaitHandle.WaitOne(5);
                }
            }
            catch (Exception ex)
            {
                if (!token.IsCancellationRequested)
                {
                    Error?.Invoke(ex);
                }
            }
        }

        private void OnSystemButton(ulong callbackToken, IntPtr context, IntPtr device, ulong timestamp, int currentButtons, int previousButtons)
        {
            var changedButtons = currentButtons ^ previousButtons;
            if ((changedButtons & GameInputSystemButtonShare) != 0)
            {
                SystemButtonChanged?.Invoke(GameInputSystemButtonShare, (currentButtons & GameInputSystemButtonShare) != 0);
            }

            if ((changedButtons & GameInputSystemButtonGuide) != 0)
            {
                SystemButtonChanged?.Invoke(GameInputSystemButtonGuide, (currentButtons & GameInputSystemButtonGuide) != 0);
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

            if (_gameInput != IntPtr.Zero && _callbackToken != 0)
            {
                UnregisterCallback(_gameInput, _callbackToken);
                _callbackToken = 0;
            }

            if (_dispatchTask != null)
            {
                try
                {
                    _dispatchTask.Wait(250);
                }
                catch
                {
                }
            }

            if (_dispatcher != IntPtr.Zero)
            {
                Release(_dispatcher);
                _dispatcher = IntPtr.Zero;
            }

            if (_gameInput != IntPtr.Zero)
            {
                Release(_gameInput);
                _gameInput = IntPtr.Zero;
            }

            _stop.Dispose();

            if (_gameInputLibrary != IntPtr.Zero)
            {
                FreeLibrary(_gameInputLibrary);
                _gameInputLibrary = IntPtr.Zero;
            }
        }

        private static void ThrowIfFailed(int hr, string message)
        {
            if (hr < 0)
            {
                throw new Win32Exception(hr, message + ", HRESULT=0x" + unchecked((uint)hr).ToString("X8"));
            }
        }

        private static T GetMethod<T>(IntPtr instance, int index) where T : class
        {
            var vtable = Marshal.ReadIntPtr(instance);
            var function = Marshal.ReadIntPtr(vtable, index * IntPtr.Size);
            return Marshal.GetDelegateForFunctionPointer(function, typeof(T)) as T;
        }

        private static void SetFocusPolicy(IntPtr gameInput, int policy)
        {
            GetMethod<SetFocusPolicyDelegate>(gameInput, 16)(gameInput, policy);
        }

        private static int CreateDispatcher(IntPtr gameInput, out IntPtr dispatcher)
        {
            return GetMethod<CreateDispatcherDelegate>(gameInput, 13)(gameInput, out dispatcher);
        }

        private static int RegisterSystemButtonCallback(
            IntPtr gameInput,
            IntPtr device,
            int buttonFilter,
            IntPtr context,
            SystemButtonCallback callback,
            out ulong callbackToken)
        {
            return GetMethod<RegisterSystemButtonCallbackDelegate>(gameInput, 9)(
                gameInput,
                device,
                buttonFilter,
                context,
                callback,
                out callbackToken);
        }

        private static bool UnregisterCallback(IntPtr gameInput, ulong callbackToken)
        {
            return GetMethod<UnregisterCallbackDelegate>(gameInput, 12)(gameInput, callbackToken);
        }

        private static bool Dispatch(IntPtr dispatcher, ulong quotaInMicroseconds)
        {
            return GetMethod<DispatchDelegate>(dispatcher, 3)(dispatcher, quotaInMicroseconds);
        }

        private static uint Release(IntPtr instance)
        {
            return GetMethod<ReleaseDelegate>(instance, 2)(instance);
        }

        private GameInputInitializeDelegate LoadGameInputInitialize()
        {
            var candidates = new[]
            {
                System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "GameInputRedist.dll"),
                System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Microsoft GameInput", "x64", "GameInputRedist.dll"),
                "GameInputRedist.dll"
            };

            foreach (var candidate in candidates)
            {
                var library = LoadLibrary(candidate);
                if (library == IntPtr.Zero)
                {
                    continue;
                }

                var proc = GetProcAddress(library, "GameInputInitialize");
                if (proc == IntPtr.Zero)
                {
                    FreeLibrary(library);
                    continue;
                }

                _gameInputLibrary = library;
                return (GameInputInitializeDelegate)Marshal.GetDelegateForFunctionPointer(proc, typeof(GameInputInitializeDelegate));
            }

            throw new EntryPointNotFoundException("无法加载 GameInputRedist.dll 中的 GameInputInitialize。");
        }

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        private delegate int GameInputInitializeDelegate(ref Guid riid, out IntPtr gameInput);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        private delegate void SetFocusPolicyDelegate(IntPtr self, int policy);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        private delegate int CreateDispatcherDelegate(IntPtr self, out IntPtr dispatcher);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        private delegate int RegisterSystemButtonCallbackDelegate(
            IntPtr self,
            IntPtr device,
            int buttonFilter,
            IntPtr context,
            SystemButtonCallback callback,
            out ulong callbackToken);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        private delegate void SystemButtonCallback(
            ulong callbackToken,
            IntPtr context,
            IntPtr device,
            ulong timestamp,
            int currentButtons,
            int previousButtons);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        [return: MarshalAs(UnmanagedType.I1)]
        private delegate bool UnregisterCallbackDelegate(IntPtr self, ulong callbackToken);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        [return: MarshalAs(UnmanagedType.I1)]
        private delegate bool DispatchDelegate(IntPtr self, ulong quotaInMicroseconds);

        [UnmanagedFunctionPointer(CallingConvention.StdCall)]
        private delegate uint ReleaseDelegate(IntPtr self);

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern IntPtr LoadLibrary(string fileName);

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Ansi)]
        private static extern IntPtr GetProcAddress(IntPtr module, string procName);

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool FreeLibrary(IntPtr module);
    }
}
