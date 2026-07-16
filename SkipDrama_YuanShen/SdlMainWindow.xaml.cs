using SimWinInput;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using WpfApp2;

namespace SkipDrama_YuanShen
{
    public partial class SdlMainWindow : Window
    {
        private const short LeftTriggerThreshold = 20000;
        private const double ComboHoldMilliseconds = 50;
        private const double ShareScreenshotCooldownMilliseconds = 500;
        private const int ShareScreenshotReleaseDelayMilliseconds = 60;
        private const double DebugSnapshotIntervalMilliseconds = 100;
        private const int DebugAxisLogStep = 1500;
        private const int MaxDebugLines = 400;
        private const double NormalWindowHeight = 520;
        private const double DebugWindowHeight = 720;

        private readonly object _gamepadGate = new object();
        private readonly object _taskGate = new object();
        private readonly object _debugGate = new object();
        private readonly CancellationTokenSource _lifetime = new CancellationTokenSource();
        private readonly SdlGamepadInputService _input = new SdlGamepadInputService();
        private readonly DirectScreenshotService _directScreenshots = new DirectScreenshotService();
        private readonly Queue<string> _debugLines = new Queue<string>();

        private GlobalHotkeyManager _hotkeys;
        private RawInputHidListener _rawInput;
        private DirectHidInputListener _directHidInput;
        private GameInputSystemButtonListener _gameInputButtons;
        private CancellationTokenSource _autoClickCts;
        private CancellationTokenSource _ltMacroCts;
        private Task _autoClickTask;
        private Task _ltMacroTask;
        private GamePadControl _physicalButtons;
        private GamePadControl _macroButtons;
        private byte _physicalLeftTrigger;
        private byte _physicalRightTrigger;
        private short _physicalLeftStickX;
        private short _physicalLeftStickY;
        private short _physicalRightStickX;
        private short _physicalRightStickY;
        private volatile bool _virtualGamepadReady;
        private volatile bool _shuttingDown;
        private bool _shutdownFinished;
        private bool _comboPressed;
        private bool _comboTriggered;
        private long _comboPressedAt;
        private bool _sharePressed;
        private bool _leftPressed;
        private bool _guidePressed;
        private int _directScreenshotRunning;
        private long _lastShareScreenshotAt;
        private volatile bool _shareScreenshotEnabled = true;
        private volatile bool _leftScreenshotEnabled;
        private volatile MacroMode _macroMode;
        private int _lastDebugButtonMask = -1;
        private short _lastDebugLeftTrigger = short.MinValue;
        private short _lastDebugRightTrigger = short.MinValue;
        private bool _lastDebugLtPressed;
        private bool _lastDebugRtPressed;
        private string _lastDebugRawButtons;
        private string _lastRawHidReport;
        private bool _sdlEventSharePressed;
        private bool _sdlEventGuidePressed;
        private bool _rawSharePressed;
        private bool _rawGuidePressed;
        private bool _gameInputSharePressed;
        private bool _gameInputGuidePressed;
        private long _lastDebugSnapshotAt;
        private bool _debugEnabled;

        public SdlMainWindow()
        {
            InitializeComponent();
            _input.SnapshotReceived += ProcessSnapshot;
            _input.ButtonChanged += OnSdlButtonChanged;
            _input.EventTrace += AppendDebugLog;
            _input.DeviceChanged += device =>
            {
                SetUi(() => DeviceText.Text = device);
                AppendDebugLog("设备: " + device);
            };
            _input.Error += ReportError;
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                var handle = new WindowInteropHelper(this).Handle;
                _hotkeys = new GlobalHotkeyManager(handle);
                _hotkeys.Register(GlobalHotkeyModifiers.Control, Key.OemQuestion, ToggleAutoClick);
                _hotkeys.Register(GlobalHotkeyModifiers.Control, Key.OemComma, () => JumpPlungeCheckBox.IsChecked = !JumpPlungeCheckBox.IsChecked);
                _hotkeys.Register(GlobalHotkeyModifiers.Control, Key.OemPeriod, () => HiddenPlungeCheckBox.IsChecked = !HiddenPlungeCheckBox.IsChecked);
                AppendDebugLog("启动: 注册全局热键完成");

                _rawInput = new RawInputHidListener(handle);
                _rawInput.HidReport += OnRawHidReport;
                _rawInput.Start();
                AppendDebugLog("启动: RawInput HID 监听已开启");

                _directHidInput = new DirectHidInputListener();
                _directHidInput.HidReport += OnDirectHidReport;
                _directHidInput.DeviceOpened += AppendDebugLog;
                _directHidInput.Error += ReportError;
                _directHidInput.Start();
                AppendDebugLog("启动: Direct HID 监听已开启");

                TryStartGameInputSystemButtons();

                // Open the physical controller before creating the virtual one so SDL cannot select its own output.
                await _input.StartAsync();

                _input.BeginVirtualOutputRegistration();
                try
                {
                    SimGamePad.Instance.Initialize();
                    SimGamePad.Instance.PlugIn();
                    await _input.CompleteVirtualOutputRegistrationAsync();
                }
                catch
                {
                    _input.CancelVirtualOutputRegistration();
                    throw;
                }
                lock (_gamepadGate)
                {
                    _virtualGamepadReady = true;
                    ApplyVirtualGamepadStateLocked();
                }
                StatusText.Text = "就绪";
                AppendDebugLog("启动: 虚拟手柄已接入");
            }
            catch (Exception ex)
            {
                ReportError(ex);
            }
        }

        private void ProcessSnapshot(SdlGamepadSnapshot snapshot)
        {
            if (_shuttingDown)
            {
                return;
            }

            ForwardPhysicalController(snapshot);
            UpdateDebugSnapshot(snapshot);
            LogInputIfChanged(snapshot);
            ProcessAutoClickCombo(snapshot);
            ProcessLtMacro(snapshot);
            ProcessSpecialButtons(snapshot);
        }

        private void ProcessAutoClickCombo(SdlGamepadSnapshot snapshot)
        {
            var combo = IsLeftTriggerPressed(snapshot) && snapshot.X;
            if (!combo)
            {
                _comboPressed = false;
                _comboTriggered = false;
                return;
            }

            if (!_comboPressed)
            {
                _comboPressed = true;
                _comboPressedAt = Stopwatch.GetTimestamp();
                AppendDebugLog("组合键: LT + X 开始按住");
                return;
            }

            var elapsedMilliseconds = (Stopwatch.GetTimestamp() - _comboPressedAt) * 1000.0 / Stopwatch.Frequency;
            if (!_comboTriggered && elapsedMilliseconds >= ComboHoldMilliseconds)
            {
                _comboTriggered = true;
                AppendDebugLog("组合键: LT + X 达到 50ms，切换 A 连点");
                SetUi(ToggleAutoClick);
            }
        }

        private void ProcessLtMacro(SdlGamepadSnapshot snapshot)
        {
            if (IsLeftTriggerPressed(snapshot) && _macroMode != MacroMode.None)
            {
                EnsureLtMacroRunning();
            }
            else
            {
                CancelLtMacro();
            }
        }

        private void ProcessSpecialButtons(SdlGamepadSnapshot snapshot)
        {
            if (snapshot.Share && !_sharePressed)
            {
                AppendDebugLog("按键: Share 按下，mapped=" + snapshot.ShareMapped + ", raw11=" + snapshot.ShareRaw + ", rawButtons=" + snapshot.RawButtons);
                SetLastInput("Share");
            }

            if (!snapshot.Share && _sharePressed)
            {
                AppendDebugLog("按键: Share 松开");
                TriggerNvidiaScreenshotAfterShareRelease("SDL");
            }

            if (snapshot.DpadLeft && !_leftPressed)
            {
                AppendDebugLog("按键: 方向键左 按下");
                SetLastInput("方向键左");
                if (_leftScreenshotEnabled)
                {
                    TriggerDirectScreenshot();
                }
            }

            if (snapshot.Guide && !_guidePressed)
            {
                AppendDebugLog("按键: Guide 按下");
                SetLastInput("Guide");
            }

            _sharePressed = snapshot.Share;
            _leftPressed = snapshot.DpadLeft;
            _guidePressed = snapshot.Guide;
        }

        private static bool IsLeftTriggerPressed(SdlGamepadSnapshot snapshot)
        {
            return snapshot.LeftTrigger >= LeftTriggerThreshold;
        }

        private void ToggleAutoClick()
        {
            if (!_virtualGamepadReady || _shuttingDown)
            {
                return;
            }

            lock (_taskGate)
            {
                if (_autoClickTask != null && !_autoClickTask.IsCompleted)
                {
                    AppendDebugLog("A 连点: 请求停止");
                    _autoClickCts.Cancel();
                    return;
                }

                _autoClickCts?.Dispose();
                _autoClickCts = CancellationTokenSource.CreateLinkedTokenSource(_lifetime.Token);
                AppendDebugLog("A 连点: 启动");
                _autoClickTask = RunAutoClickAsync(_autoClickCts.Token);
            }
        }

        private async Task RunAutoClickAsync(CancellationToken token)
        {
            var count = 0;
            SetStatus("A 连点运行中", string.Empty);

            try
            {
                while (!token.IsCancellationRequested)
                {
                    GamepadButtonDown(GamePadControl.A);
                    await Task.Delay(50, token);
                    GamepadButtonUp(GamePadControl.A);
                    count++;
                    SetStatus("A 连点运行中", "  " + count);
                    if (count % 20 == 0)
                    {
                        AppendDebugLog("A 连点: 已输出 " + count + " 次");
                    }
                    await Task.Delay(30, token);
                }
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception ex)
            {
                ReportError(ex);
            }
            finally
            {
                GamepadButtonUp(GamePadControl.A);
                SetStatus("暂停", string.Empty);
                AppendDebugLog("A 连点: 已停止");
            }
        }

        private void EnsureLtMacroRunning()
        {
            if (!_virtualGamepadReady || _shuttingDown)
            {
                return;
            }

            lock (_taskGate)
            {
                if (_ltMacroTask != null && !_ltMacroTask.IsCompleted)
                {
                    return;
                }

                _ltMacroCts?.Dispose();
                _ltMacroCts = CancellationTokenSource.CreateLinkedTokenSource(_lifetime.Token);
                var mode = _macroMode;
                AppendDebugLog("LT 宏: 准备启动 " + MacroModeText(mode));
                _ltMacroTask = RunLtMacroAsync(mode, _ltMacroCts.Token);
            }
        }

        private void CancelLtMacro()
        {
            lock (_taskGate)
            {
                if (_ltMacroTask != null && !_ltMacroTask.IsCompleted)
                {
                    if (_ltMacroCts != null && !_ltMacroCts.IsCancellationRequested)
                    {
                        AppendDebugLog("LT 宏: 请求停止");
                        _ltMacroCts.Cancel();
                    }
                }
            }
        }

        private async Task RunLtMacroAsync(MacroMode mode, CancellationToken token)
        {
            var modeText = MacroModeText(mode);
            var cycle = 0;
            SetStatus(modeText + "宏运行中", string.Empty);
            AppendDebugLog("LT 宏: 已启动 " + modeText);

            try
            {
                while (!token.IsCancellationRequested)
                {
                    cycle++;
                    AppendDebugLog("LT 宏: " + modeText + " 第 " + cycle + " 轮");
                    if (mode == MacroMode.HiddenPlunge)
                    {
                        await RunHiddenPlungeCycleAsync(token);
                    }
                    else
                    {
                        await RunJumpPlungeCycleAsync(token);
                    }
                }
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception ex)
            {
                ReportError(ex);
            }
            finally
            {
                ReleaseAllMacroButtons();
                SetStatus("宏已停止", string.Empty);
                AppendDebugLog("LT 宏: 已停止 " + modeText);
            }
        }

        private async Task RunHiddenPlungeCycleAsync(CancellationToken token)
        {
            GamepadButtonDown(GamePadControl.B, "藏劈 1: B 按下 200ms");
            await Task.Delay(200, token);
            await PressForAsync(GamePadControl.RightShoulder, 50, token, "藏劈 1: RB 点击 50ms");
            await Task.Delay(70, token);
            GamepadButtonUp(GamePadControl.B, "藏劈 1: B 松开");
            await Task.Delay(50, token);

            GamepadButtonDown(GamePadControl.B, "藏劈 2: B 按下 200ms");
            await Task.Delay(200, token);
            await PressForAsync(GamePadControl.RightShoulder, 50, token, "藏劈 2: RB 点击 50ms");
            await Task.Delay(70, token);
            GamepadButtonUp(GamePadControl.B, "藏劈 2: B 松开");
            await Task.Delay(50, token);

            await PressForAsync(GamePadControl.B, 50, token, "藏劈 3: B 点击 50ms");
            await Task.Delay(50, token);
            await PressForAsync(GamePadControl.B, 50, token, "藏劈 4: B 点击 50ms");
            await Task.Delay(1050, token);
            await Task.Delay(400, token);
        }

        private async Task RunJumpPlungeCycleAsync(CancellationToken token)
        {
            GamepadButtonDown(GamePadControl.B, "跳劈 1: B 按下 190ms");
            await Task.Delay(190, token);
            await PressForAsync(GamePadControl.RightShoulder, 50, token, "跳劈 1: RB 点击 50ms");
            await Task.Delay(70, token);
            GamepadButtonUp(GamePadControl.B, "跳劈 1: B 松开");
            await Task.Delay(50, token);

            GamepadButtonDown(GamePadControl.B, "跳劈 2: B 按下 200ms");
            await Task.Delay(200, token);
            await PressForAsync(GamePadControl.RightShoulder, 50, token, "跳劈 2: RB 点击 50ms");
            await Task.Delay(980, token);
            GamepadButtonUp(GamePadControl.B, "跳劈 2: B 松开");
            await Task.Delay(800, token);
        }

        private async Task PressForAsync(GamePadControl button, int milliseconds, CancellationToken token, string debugAction)
        {
            GamepadButtonDown(button, debugAction);
            await Task.Delay(milliseconds, token);
            GamepadButtonUp(button, debugAction);
        }

        private void GamepadButtonDown(GamePadControl button, string debugAction = null)
        {
            if (!string.IsNullOrEmpty(debugAction))
            {
                AppendDebugLog("虚拟手柄: " + debugAction + " -> " + ControlText(button) + " Down");
            }

            lock (_gamepadGate)
            {
                _macroButtons |= button;
                ApplyVirtualGamepadStateLocked();
            }
        }

        private void GamepadButtonUp(GamePadControl button, string debugAction = null)
        {
            if (!string.IsNullOrEmpty(debugAction))
            {
                AppendDebugLog("虚拟手柄: " + debugAction + " -> " + ControlText(button) + " Up");
            }

            lock (_gamepadGate)
            {
                _macroButtons &= ~button;
                ApplyVirtualGamepadStateLocked();
            }
        }

        private void ForwardPhysicalController(SdlGamepadSnapshot snapshot)
        {
            lock (_gamepadGate)
            {
                _physicalButtons = BuildVirtualButtons(snapshot);
                _physicalLeftTrigger = ScaleTrigger(snapshot.LeftTrigger);
                _physicalRightTrigger = ScaleTrigger(snapshot.RightTrigger);
                _physicalLeftStickX = snapshot.LeftX;
                _physicalLeftStickY = InvertStickY(snapshot.LeftY);
                _physicalRightStickX = snapshot.RightX;
                _physicalRightStickY = InvertStickY(snapshot.RightY);
                ApplyVirtualGamepadStateLocked();
            }
        }

        private void ApplyVirtualGamepadStateLocked()
        {
            if (!_virtualGamepadReady)
            {
                return;
            }

            var state = SimGamePad.Instance.State[0];
            state.Buttons = _physicalButtons | _macroButtons;
            state.LeftTrigger = _physicalLeftTrigger;
            state.RightTrigger = _physicalRightTrigger;
            state.LeftStickX = _physicalLeftStickX;
            state.LeftStickY = _physicalLeftStickY;
            state.RightStickX = _physicalRightStickX;
            state.RightStickY = _physicalRightStickY;
            SimGamePad.Instance.Update(0);
        }

        private static GamePadControl BuildVirtualButtons(SdlGamepadSnapshot snapshot)
        {
            var buttons = GamePadControl.None;
            if (snapshot.A) buttons |= GamePadControl.A;
            if (snapshot.B) buttons |= GamePadControl.B;
            if (snapshot.X) buttons |= GamePadControl.X;
            if (snapshot.Y) buttons |= GamePadControl.Y;
            if (snapshot.Start) buttons |= GamePadControl.Start;
            if (snapshot.Back) buttons |= GamePadControl.Back;
            if (snapshot.LeftStickClick) buttons |= GamePadControl.LeftStickClick;
            if (snapshot.RightStickClick) buttons |= GamePadControl.RightStickClick;
            if (snapshot.LeftShoulder) buttons |= GamePadControl.LeftShoulder;
            if (snapshot.RightShoulder) buttons |= GamePadControl.RightShoulder;
            if (snapshot.DpadUp) buttons |= GamePadControl.DPadUp;
            if (snapshot.DpadDown) buttons |= GamePadControl.DPadDown;
            if (snapshot.DpadLeft) buttons |= GamePadControl.DPadLeft;
            if (snapshot.DpadRight) buttons |= GamePadControl.DPadRight;
            if (snapshot.Guide) buttons |= GamePadControl.Guide;
            return buttons;
        }

        private static byte ScaleTrigger(short value)
        {
            var positive = Math.Max(0, (int)value);
            return (byte)Math.Min(255, positive * 255 / short.MaxValue);
        }

        private static short InvertStickY(short value)
        {
            return value == short.MinValue ? short.MaxValue : (short)-value;
        }

        private void ReleaseAllMacroButtons()
        {
            AppendDebugLog("虚拟手柄: 释放宏按键 A/B/RB");
            GamepadButtonUp(GamePadControl.A);
            GamepadButtonUp(GamePadControl.B);
            GamepadButtonUp(GamePadControl.RightShoulder);
        }

        private void TriggerNvidiaScreenshot(string source)
        {
            try
            {
                if (!AcceptShareScreenshotTrigger())
                {
                    AppendDebugLog("截图: Share 触发被忽略，仍在冷却时间内");
                    return;
                }

                AppendDebugLog("截图: 发送 NVIDIA Alt+F1，来源=" + source);
                var result = KeyboardShortcutService.SendNvidiaScreenshotShortcut();
                AppendDebugLog("截图: NVIDIA Alt+F1 发送结果: " + result);
                SetStatus("已发送 NVIDIA 截图快捷键", "  Alt+F1 (" + source + ")");
            }
            catch (Exception ex)
            {
                ReportError(ex);
            }
        }

        private void TriggerNvidiaScreenshotAfterShareRelease(string source)
        {
            if (!_shareScreenshotEnabled)
            {
                AppendDebugLog("截图: Share -> NVIDIA 已关闭");
                return;
            }

            Task.Run(() =>
            {
                Thread.Sleep(ShareScreenshotReleaseDelayMilliseconds);
                TriggerNvidiaScreenshot(source + " release");
            });
        }

        private bool AcceptShareScreenshotTrigger()
        {
            var now = Stopwatch.GetTimestamp();
            var last = Interlocked.Read(ref _lastShareScreenshotAt);
            var elapsedMilliseconds = (now - last) * 1000.0 / Stopwatch.Frequency;
            if (last != 0 && elapsedMilliseconds < ShareScreenshotCooldownMilliseconds)
            {
                return false;
            }

            Interlocked.Exchange(ref _lastShareScreenshotAt, now);
            return true;
        }

        private async void TriggerDirectScreenshot()
        {
            TriggerDirectScreenshot("方向键左");
        }

        private async void TriggerDirectScreenshot(string source)
        {
            if (Interlocked.Exchange(ref _directScreenshotRunning, 1) != 0)
            {
                return;
            }

            try
            {
                AppendDebugLog("截图: 程序直接截图开始");
                var path = await _directScreenshots.CapturePrimaryScreenAsync(_lifetime.Token);
                SetStatus("程序截图已保存", "  " + System.IO.Path.GetFileName(path));
                AppendDebugLog("截图: 程序直接截图已保存 (" + source + ") " + path);
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception ex)
            {
                ReportError(ex);
            }
            finally
            {
                Interlocked.Exchange(ref _directScreenshotRunning, 0);
            }
        }

        private void JumpPlunge_Checked(object sender, RoutedEventArgs e)
        {
            if (_macroMode != MacroMode.JumpPlunge)
            {
                CancelLtMacro();
            }
            _macroMode = MacroMode.JumpPlunge;
            AppendDebugLog("宏开关: 火神跳劈 已启用");
            if (HiddenPlungeCheckBox != null)
            {
                HiddenPlungeCheckBox.IsChecked = false;
            }
        }

        private void JumpPlunge_Unchecked(object sender, RoutedEventArgs e)
        {
            if (_macroMode == MacroMode.JumpPlunge)
            {
                _macroMode = MacroMode.None;
                CancelLtMacro();
                AppendDebugLog("宏开关: 火神跳劈 已关闭");
            }
        }

        private void HiddenPlunge_Checked(object sender, RoutedEventArgs e)
        {
            if (_macroMode != MacroMode.HiddenPlunge)
            {
                CancelLtMacro();
            }
            _macroMode = MacroMode.HiddenPlunge;
            AppendDebugLog("宏开关: 藏劈 已启用");
            if (JumpPlungeCheckBox != null)
            {
                JumpPlungeCheckBox.IsChecked = false;
            }
        }

        private void HiddenPlunge_Unchecked(object sender, RoutedEventArgs e)
        {
            if (_macroMode == MacroMode.HiddenPlunge)
            {
                _macroMode = MacroMode.None;
                CancelLtMacro();
                AppendDebugLog("宏开关: 藏劈 已关闭");
            }
        }

        private void ScreenshotOption_Changed(object sender, RoutedEventArgs e)
        {
            _shareScreenshotEnabled = ShareScreenshotCheckBox?.IsChecked == true;
            _leftScreenshotEnabled = LeftScreenshotCheckBox?.IsChecked == true;
            AppendDebugLog("截图开关: Share->NVIDIA=" + _shareScreenshotEnabled + ", 方向键左->直接截图=" + _leftScreenshotEnabled);
        }

        private async void Window_Closing(object sender, CancelEventArgs e)
        {
            if (_shutdownFinished)
            {
                return;
            }

            e.Cancel = true;
            if (_shuttingDown)
            {
                return;
            }

            _shuttingDown = true;
            StatusText.Text = "正在关闭...";
            await ShutdownAsync();
            _shutdownFinished = true;
            Close();
        }

        private async Task ShutdownAsync()
        {
            _hotkeys?.Dispose();
            if (_rawInput != null)
            {
                _rawInput.HidReport -= OnRawHidReport;
                _rawInput.Dispose();
                _rawInput = null;
            }
            if (_directHidInput != null)
            {
                _directHidInput.HidReport -= OnDirectHidReport;
                _directHidInput.DeviceOpened -= AppendDebugLog;
                _directHidInput.Error -= ReportError;
                _directHidInput.Dispose();
                _directHidInput = null;
            }
            if (_gameInputButtons != null)
            {
                _gameInputButtons.SystemButtonChanged -= OnGameInputSystemButtonChanged;
                _gameInputButtons.StatusChanged -= AppendDebugLog;
                _gameInputButtons.Error -= ReportError;
                _gameInputButtons.Dispose();
                _gameInputButtons = null;
            }
            _input.ButtonChanged -= OnSdlButtonChanged;
            _input.EventTrace -= AppendDebugLog;
            _lifetime.Cancel();

            lock (_taskGate)
            {
                _autoClickCts?.Cancel();
                _ltMacroCts?.Cancel();
            }

            await AwaitWithoutFailure(_autoClickTask);
            await AwaitWithoutFailure(_ltMacroTask);
            ReleaseAllMacroButtons();
            await AwaitWithoutFailure(_input.StopAsync());

            lock (_gamepadGate)
            {
                if (_virtualGamepadReady)
                {
                    SimGamePad.Instance.Unplug();
                    SimGamePad.Instance.ShutDown();
                    _virtualGamepadReady = false;
                }
            }

            _input.Dispose();
            _directScreenshots.Dispose();
            _autoClickCts?.Dispose();
            _ltMacroCts?.Dispose();
            _lifetime.Dispose();
        }

        private static async Task AwaitWithoutFailure(Task task)
        {
            if (task == null)
            {
                return;
            }

            try
            {
                await task;
            }
            catch (OperationCanceledException)
            {
            }
            catch
            {
                // Shutdown continues so the virtual controller can always be unplugged.
            }
        }

        private void TryStartGameInputSystemButtons()
        {
            try
            {
                _gameInputButtons = new GameInputSystemButtonListener();
                _gameInputButtons.SystemButtonChanged += OnGameInputSystemButtonChanged;
                _gameInputButtons.StatusChanged += AppendDebugLog;
                _gameInputButtons.Error += ReportError;
                _gameInputButtons.Start();
                AppendDebugLog("启动: GameInput system button 监听已开启");
            }
            catch (Exception ex)
            {
                if (_gameInputButtons != null)
                {
                    _gameInputButtons.SystemButtonChanged -= OnGameInputSystemButtonChanged;
                    _gameInputButtons.StatusChanged -= AppendDebugLog;
                    _gameInputButtons.Error -= ReportError;
                    _gameInputButtons.Dispose();
                    _gameInputButtons = null;
                }

                AppendDebugLog("GameInput 启动失败: " + ex.Message);
            }
        }

        private void OnRawHidReport(object sender, HidReportEventArgs e)
        {
            ProcessHidReport(e.Report, "RawInput HID");
        }

        private void OnDirectHidReport(object sender, HidReportEventArgs e)
        {
            ProcessHidReport(e.Report, "Direct HID");
        }

        private void ProcessHidReport(byte[] report, string source)
        {
            if (report == null || report.Length == 0)
            {
                return;
            }

            var reportText = BitConverter.ToString(report);
            if (!string.Equals(reportText, _lastRawHidReport, StringComparison.Ordinal))
            {
                _lastRawHidReport = reportText;
                AppendDebugLog(source + ": bytes=" + report.Length + ", data=" + reportText);
            }

            var share = IsRawHidSharePressed(report);
            var guide = IsRawHidGuidePressed(report);

            if (share && !_rawSharePressed)
            {
                AppendDebugLog(source + ": Share 按下，shareByte=0x" + GetRawHidShareGuideByte(report).ToString("X2"));
                SetLastInput("Share (" + source + ")");
            }

            if (!share && _rawSharePressed)
            {
                AppendDebugLog(source + ": Share 松开");
                TriggerNvidiaScreenshotAfterShareRelease(source);
            }

            if (guide && !_rawGuidePressed)
            {
                AppendDebugLog(source + ": Guide 按下，shareByte=0x" + GetRawHidShareGuideByte(report).ToString("X2"));
                SetLastInput("Guide (" + source + ")");
            }

            if (!guide && _rawGuidePressed)
            {
                AppendDebugLog(source + ": Guide 松开");
            }

            _rawSharePressed = share;
            _rawGuidePressed = guide;
        }

        private void OnGameInputSystemButtonChanged(int button, bool pressed)
        {
            if (button == 2)
            {
                if (pressed && !_gameInputSharePressed)
                {
                    AppendDebugLog("GameInput: Share 按下");
                    SetLastInput("Share (GameInput)");
                }

                if (!pressed && _gameInputSharePressed)
                {
                    AppendDebugLog("GameInput: Share 松开");
                    if (_sharePressed || _sdlEventSharePressed || _rawSharePressed)
                    {
                        AppendDebugLog("GameInput: SDL/Raw 已接管 Share，等待 SDL/Raw 松开触发");
                    }
                    else
                    {
                        TriggerNvidiaScreenshotAfterShareRelease("GameInput");
                    }
                }

                _gameInputSharePressed = pressed;
                return;
            }

            if (button == 1)
            {
                if (pressed && !_gameInputGuidePressed)
                {
                    AppendDebugLog("GameInput: Guide 按下");
                    SetLastInput("Guide (GameInput)");
                }

                if (!pressed && _gameInputGuidePressed)
                {
                    AppendDebugLog("GameInput: Guide 松开");
                }

                _gameInputGuidePressed = pressed;
            }
        }

        private void OnSdlButtonChanged(SdlButtonChangedEvent e)
        {
            if (e.Share)
            {
                if (e.Pressed && !_sdlEventSharePressed)
                {
                    AppendDebugLog(e.Source + ": Share 按下，button=" + e.Button + ", device=" + e.DeviceId);
                    SetLastInput("Share (" + e.Source + ")");
                }

                if (!e.Pressed && _sdlEventSharePressed)
                {
                    AppendDebugLog(e.Source + ": Share 松开，button=" + e.Button + ", device=" + e.DeviceId);
                    TriggerNvidiaScreenshotAfterShareRelease(e.Source);
                }

                _sdlEventSharePressed = e.Pressed;
                return;
            }

            if (e.Guide)
            {
                if (e.Pressed && !_sdlEventGuidePressed)
                {
                    AppendDebugLog(e.Source + ": Guide 按下，button=" + e.Button + ", device=" + e.DeviceId);
                    SetLastInput("Guide (" + e.Source + ")");
                }

                if (!e.Pressed && _sdlEventGuidePressed)
                {
                    AppendDebugLog(e.Source + ": Guide 松开，button=" + e.Button + ", device=" + e.DeviceId);
                }

                _sdlEventGuidePressed = e.Pressed;
            }
        }

        private static bool IsRawHidSharePressed(byte[] report)
        {
            return IsXboxOneShareGuideReport(report) && (GetRawHidShareGuideByte(report) & 0x08) != 0;
        }

        private static bool IsRawHidGuidePressed(byte[] report)
        {
            return IsXboxOneShareGuideReport(report) && (GetRawHidShareGuideByte(report) & 0x04) != 0;
        }

        private static bool IsXboxOneShareGuideReport(byte[] report)
        {
            // Xbox One controllers expose Share/Guide in this 16-byte HID report. UU remote
            // presents an Xbox 360-style controller whose DPad bits can otherwise look identical.
            return (report.Length == 16 && report[0] == 0x00 && report[1] != 0x00) ||
                   (report.Length == 17 && report[0] == 0x00 && report[1] == 0x00 && report[2] != 0x00);
        }

        private static byte GetRawHidShareGuideByte(byte[] report)
        {
            return report.Length == 17 ? report[13] : report[12];
        }

        private void UpdateDebugSnapshot(SdlGamepadSnapshot snapshot)
        {
            if (!_debugEnabled)
            {
                return;
            }

            var now = Stopwatch.GetTimestamp();
            if (_lastDebugSnapshotAt != 0)
            {
                var elapsedMilliseconds = (now - _lastDebugSnapshotAt) * 1000.0 / Stopwatch.Frequency;
                if (elapsedMilliseconds < DebugSnapshotIntervalMilliseconds)
                {
                    return;
                }
            }

            _lastDebugSnapshotAt = now;
            var text = FormatSnapshot(snapshot, includeRawAxes: true);
            SetUi(() =>
            {
                if (DebugSnapshotText != null)
                {
                    DebugSnapshotText.Text = text;
                }
            });
        }

        private void LogInputIfChanged(SdlGamepadSnapshot snapshot)
        {
            if (!_debugEnabled)
            {
                return;
            }

            var mask = BuildButtonMask(snapshot);
            var ltPressed = IsLeftTriggerPressed(snapshot);
            var rtPressed = snapshot.RightTrigger >= LeftTriggerThreshold;
            var first = _lastDebugButtonMask < 0;
            var axisChanged = first ||
                              Math.Abs(snapshot.LeftTrigger - _lastDebugLeftTrigger) >= DebugAxisLogStep ||
                              Math.Abs(snapshot.RightTrigger - _lastDebugRightTrigger) >= DebugAxisLogStep;
            var triggerStateChanged = first || ltPressed != _lastDebugLtPressed || rtPressed != _lastDebugRtPressed;
            var buttonChanged = first || mask != _lastDebugButtonMask;
            var rawButtonChanged = first || !string.Equals(snapshot.RawButtons, _lastDebugRawButtons, StringComparison.Ordinal);

            if (axisChanged || triggerStateChanged || buttonChanged || rawButtonChanged)
            {
                AppendDebugLog("输入变化: " + FormatSnapshot(snapshot, includeRawAxes: false));
                _lastDebugButtonMask = mask;
                _lastDebugLeftTrigger = snapshot.LeftTrigger;
                _lastDebugRightTrigger = snapshot.RightTrigger;
                _lastDebugLtPressed = ltPressed;
                _lastDebugRtPressed = rtPressed;
                _lastDebugRawButtons = snapshot.RawButtons;
            }
        }

        private void AppendDebugLog(string message)
        {
            if (!_debugEnabled)
            {
                return;
            }

            var line = DateTime.Now.ToString("HH:mm:ss.fff") + "  " + message;
            string text;
            lock (_debugGate)
            {
                _debugLines.Enqueue(line);
                while (_debugLines.Count > MaxDebugLines)
                {
                    _debugLines.Dequeue();
                }

                text = string.Join(Environment.NewLine, _debugLines);
            }

            SetUi(() =>
            {
                if (DebugTextBox == null)
                {
                    return;
                }

                DebugTextBox.Text = text;
                DebugTextBox.CaretIndex = DebugTextBox.Text.Length;
                DebugTextBox.ScrollToEnd();
            });
        }

        private void ToggleDebugButton_Click(object sender, RoutedEventArgs e)
        {
            _debugEnabled = !_debugEnabled;
            if (_debugEnabled)
            {
                DebugGroupBox.Visibility = Visibility.Visible;
                DebugToggleButton.Content = "隐藏调试";
                MinHeight = 560;
                if (Height < DebugWindowHeight)
                {
                    Height = DebugWindowHeight;
                }

                _lastDebugButtonMask = -1;
                _lastDebugLeftTrigger = short.MinValue;
                _lastDebugRightTrigger = short.MinValue;
                _lastDebugRawButtons = null;
                _lastRawHidReport = null;
                _lastDebugSnapshotAt = 0;
                DebugSnapshotText.Text = "等待输入...";
                AppendDebugLog("调试输出已启用");
            }
            else
            {
                AppendDebugLog("调试输出已关闭");
                DebugGroupBox.Visibility = Visibility.Collapsed;
                DebugToggleButton.Content = "显示调试";
                MinHeight = 460;
                Height = NormalWindowHeight;
            }
        }

        private void ClearDebugButton_Click(object sender, RoutedEventArgs e)
        {
            lock (_debugGate)
            {
                _debugLines.Clear();
            }

            DebugTextBox.Clear();
            AppendDebugLog("调试日志已清空");
        }

        private static string FormatSnapshot(SdlGamepadSnapshot snapshot, bool includeRawAxes)
        {
            var text = "Buttons=[" + FormatPressedButtons(snapshot) + "] " +
                       "LT=" + snapshot.LeftTrigger + " (mapped=" + snapshot.LeftTriggerMapped + ", pressed=" + IsLeftTriggerPressed(snapshot) + ") " +
                       "RT=" + snapshot.RightTrigger + " (mapped=" + snapshot.RightTriggerMapped + ") " +
                       "Share=" + snapshot.Share + " (mapped=" + snapshot.ShareMapped + ", raw11=" + snapshot.ShareRaw + ") " +
                       "RawButtons=[" + ValueOrDash(snapshot.RawButtons) + "]";

            if (includeRawAxes)
            {
                text += " RawAxes=[" + ValueOrDash(snapshot.RawAxes) + "]";
            }

            return text;
        }

        private static string FormatPressedButtons(SdlGamepadSnapshot snapshot)
        {
            var buttons = new List<string>();
            if (snapshot.A) buttons.Add("A");
            if (snapshot.B) buttons.Add("B");
            if (snapshot.X) buttons.Add("X");
            if (snapshot.Y) buttons.Add("Y");
            if (snapshot.Start) buttons.Add("Start");
            if (snapshot.Back) buttons.Add("Back");
            if (snapshot.LeftStickClick) buttons.Add("L3");
            if (snapshot.RightStickClick) buttons.Add("R3");
            if (snapshot.LeftShoulder) buttons.Add("LB");
            if (snapshot.RightShoulder) buttons.Add("RB");
            if (snapshot.DpadLeft) buttons.Add("DPadLeft");
            if (snapshot.DpadRight) buttons.Add("DPadRight");
            if (snapshot.DpadUp) buttons.Add("DPadUp");
            if (snapshot.DpadDown) buttons.Add("DPadDown");
            if (snapshot.Guide) buttons.Add("Guide");
            if (snapshot.Share) buttons.Add("Share");
            return buttons.Count == 0 ? "-" : string.Join("+", buttons);
        }

        private static int BuildButtonMask(SdlGamepadSnapshot snapshot)
        {
            var mask = 0;
            if (snapshot.A) mask |= 1 << 0;
            if (snapshot.B) mask |= 1 << 1;
            if (snapshot.X) mask |= 1 << 2;
            if (snapshot.Y) mask |= 1 << 3;
            if (snapshot.Start) mask |= 1 << 4;
            if (snapshot.Back) mask |= 1 << 5;
            if (snapshot.LeftStickClick) mask |= 1 << 6;
            if (snapshot.RightStickClick) mask |= 1 << 7;
            if (snapshot.LeftShoulder) mask |= 1 << 8;
            if (snapshot.RightShoulder) mask |= 1 << 9;
            if (snapshot.DpadLeft) mask |= 1 << 10;
            if (snapshot.DpadRight) mask |= 1 << 11;
            if (snapshot.DpadUp) mask |= 1 << 12;
            if (snapshot.DpadDown) mask |= 1 << 13;
            if (snapshot.Guide) mask |= 1 << 14;
            if (snapshot.Share) mask |= 1 << 15;
            return mask;
        }

        private static string ValueOrDash(string value)
        {
            return string.IsNullOrEmpty(value) ? "-" : value;
        }

        private static string MacroModeText(MacroMode mode)
        {
            switch (mode)
            {
                case MacroMode.JumpPlunge:
                    return "火神跳劈";
                case MacroMode.HiddenPlunge:
                    return "藏劈";
                default:
                    return "无";
            }
        }

        private static string ControlText(GamePadControl control)
        {
            switch (control)
            {
                case GamePadControl.A:
                    return "A";
                case GamePadControl.B:
                    return "B";
                case GamePadControl.RightShoulder:
                    return "RB";
                default:
                    return control.ToString();
            }
        }

        private void SetStatus(string status, string detail)
        {
            SetUi(() =>
            {
                StatusText.Text = status;
                CountText.Text = detail;
            });
        }

        private void SetLastInput(string input)
        {
            SetUi(() => LastInputText.Text = input);
        }

        private void ReportError(Exception exception)
        {
            SetUi(() => ErrorText.Text = exception.Message);
            AppendDebugLog("错误: " + exception.Message);
        }

        private void SetUi(Action action)
        {
            if (!Dispatcher.HasShutdownStarted)
            {
                Dispatcher.BeginInvoke(action);
            }
        }

        private enum MacroMode
        {
            None,
            JumpPlunge,
            HiddenPlunge
        }
    }
}
