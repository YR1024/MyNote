using SimWinInput;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;

namespace SkipDrama_YuanShen
{
    public partial class SdlMainWindow : Window
    {
        private const short LeftTriggerThreshold = 3855;
        private const double ComboHoldMilliseconds = 50;

        private readonly object _gamepadGate = new object();
        private readonly object _taskGate = new object();
        private readonly CancellationTokenSource _lifetime = new CancellationTokenSource();
        private readonly SdlGamepadInputService _input = new SdlGamepadInputService();
        private readonly DirectScreenshotService _directScreenshots = new DirectScreenshotService();

        private GlobalHotkeyManager _hotkeys;
        private CancellationTokenSource _autoClickCts;
        private CancellationTokenSource _ltMacroCts;
        private Task _autoClickTask;
        private Task _ltMacroTask;
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
        private volatile bool _shareScreenshotEnabled = true;
        private volatile bool _leftScreenshotEnabled = true;
        private volatile MacroMode _macroMode;

        public SdlMainWindow()
        {
            InitializeComponent();
            _input.SnapshotReceived += ProcessSnapshot;
            _input.DeviceChanged += device => SetUi(() => DeviceText.Text = device);
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
                _virtualGamepadReady = true;
                StatusText.Text = "就绪";
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

            var gameIsForeground = ForegroundGameGuard.IsGenshinForeground();
            ProcessAutoClickCombo(snapshot, gameIsForeground);
            ProcessLtMacro(snapshot, gameIsForeground);
            ProcessSpecialButtons(snapshot);
        }

        private void ProcessAutoClickCombo(SdlGamepadSnapshot snapshot, bool gameIsForeground)
        {
            var combo = snapshot.LeftTrigger > LeftTriggerThreshold && snapshot.X;
            if (!combo || !gameIsForeground)
            {
                _comboPressed = false;
                _comboTriggered = false;
                return;
            }

            if (!_comboPressed)
            {
                _comboPressed = true;
                _comboPressedAt = Stopwatch.GetTimestamp();
                return;
            }

            var elapsedMilliseconds = (Stopwatch.GetTimestamp() - _comboPressedAt) * 1000.0 / Stopwatch.Frequency;
            if (!_comboTriggered && elapsedMilliseconds >= ComboHoldMilliseconds)
            {
                _comboTriggered = true;
                SetUi(ToggleAutoClick);
            }
        }

        private void ProcessLtMacro(SdlGamepadSnapshot snapshot, bool gameIsForeground)
        {
            if (snapshot.LeftTrigger > LeftTriggerThreshold && _macroMode != MacroMode.None && gameIsForeground)
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
                SetLastInput("Share");
                if (_shareScreenshotEnabled)
                {
                    TriggerNvidiaScreenshot();
                }
            }

            if (snapshot.DpadLeft && !_leftPressed)
            {
                SetLastInput("方向键左");
                if (_leftScreenshotEnabled)
                {
                    TriggerDirectScreenshot();
                }
            }

            if (snapshot.Guide && !_guidePressed)
            {
                SetLastInput("Guide");
            }

            _sharePressed = snapshot.Share;
            _leftPressed = snapshot.DpadLeft;
            _guidePressed = snapshot.Guide;
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
                    _autoClickCts.Cancel();
                    return;
                }

                _autoClickCts?.Dispose();
                _autoClickCts = CancellationTokenSource.CreateLinkedTokenSource(_lifetime.Token);
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
                    if (!ForegroundGameGuard.IsGenshinForeground())
                    {
                        GamepadButtonUp(GamePadControl.A);
                        await Task.Delay(100, token);
                        continue;
                    }

                    GamepadButtonDown(GamePadControl.A);
                    await Task.Delay(50, token);
                    GamepadButtonUp(GamePadControl.A);
                    count++;
                    SetStatus("A 连点运行中", "  " + count);
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
                _ltMacroTask = RunLtMacroAsync(mode, _ltMacroCts.Token);
            }
        }

        private void CancelLtMacro()
        {
            lock (_taskGate)
            {
                if (_ltMacroTask != null && !_ltMacroTask.IsCompleted)
                {
                    _ltMacroCts.Cancel();
                }
            }
        }

        private async Task RunLtMacroAsync(MacroMode mode, CancellationToken token)
        {
            SetStatus(mode == MacroMode.HiddenPlunge ? "藏劈宏运行中" : "跳劈宏运行中", string.Empty);

            try
            {
                while (!token.IsCancellationRequested)
                {
                    if (!ForegroundGameGuard.IsGenshinForeground())
                    {
                        break;
                    }

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
            }
        }

        private async Task RunHiddenPlungeCycleAsync(CancellationToken token)
        {
            GamepadButtonDown(GamePadControl.B);
            await Task.Delay(200, token);
            await PressForAsync(GamePadControl.RightShoulder, 50, token);
            await Task.Delay(70, token);
            GamepadButtonUp(GamePadControl.B);
            await Task.Delay(50, token);

            GamepadButtonDown(GamePadControl.B);
            await Task.Delay(200, token);
            await PressForAsync(GamePadControl.RightShoulder, 50, token);
            await Task.Delay(70, token);
            GamepadButtonUp(GamePadControl.B);
            await Task.Delay(50, token);

            await PressForAsync(GamePadControl.B, 50, token);
            await PressForAsync(GamePadControl.B, 50, token);
            await Task.Delay(1450, token);
        }

        private async Task RunJumpPlungeCycleAsync(CancellationToken token)
        {
            GamepadButtonDown(GamePadControl.B);
            await Task.Delay(190, token);
            await PressForAsync(GamePadControl.RightShoulder, 50, token);
            await Task.Delay(70, token);
            GamepadButtonUp(GamePadControl.B);
            await Task.Delay(50, token);

            GamepadButtonDown(GamePadControl.B);
            await Task.Delay(200, token);
            await PressForAsync(GamePadControl.RightShoulder, 50, token);
            await Task.Delay(980, token);
            GamepadButtonUp(GamePadControl.B);
            await Task.Delay(800, token);
        }

        private async Task PressForAsync(GamePadControl button, int milliseconds, CancellationToken token)
        {
            GamepadButtonDown(button);
            await Task.Delay(milliseconds, token);
            GamepadButtonUp(button);
        }

        private void GamepadButtonDown(GamePadControl button)
        {
            lock (_gamepadGate)
            {
                if (_virtualGamepadReady)
                {
                    SimGamePad.Instance.SetControl(button, 0);
                }
            }
        }

        private void GamepadButtonUp(GamePadControl button)
        {
            lock (_gamepadGate)
            {
                if (_virtualGamepadReady)
                {
                    SimGamePad.Instance.ReleaseControl(button, 0);
                }
            }
        }

        private void ReleaseAllMacroButtons()
        {
            GamepadButtonUp(GamePadControl.A);
            GamepadButtonUp(GamePadControl.B);
            GamepadButtonUp(GamePadControl.RightShoulder);
        }

        private void TriggerNvidiaScreenshot()
        {
            try
            {
                KeyboardShortcutService.SendNvidiaScreenshotShortcut();
                SetStatus("已发送 NVIDIA 截图快捷键", "  Alt+F1");
            }
            catch (Exception ex)
            {
                ReportError(ex);
            }
        }

        private async void TriggerDirectScreenshot()
        {
            if (Interlocked.Exchange(ref _directScreenshotRunning, 1) != 0)
            {
                return;
            }

            try
            {
                var path = await _directScreenshots.CapturePrimaryScreenAsync(_lifetime.Token);
                SetStatus("程序截图已保存", "  " + System.IO.Path.GetFileName(path));
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
            }
        }

        private void HiddenPlunge_Checked(object sender, RoutedEventArgs e)
        {
            if (_macroMode != MacroMode.HiddenPlunge)
            {
                CancelLtMacro();
            }
            _macroMode = MacroMode.HiddenPlunge;
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
            }
        }

        private void ScreenshotOption_Changed(object sender, RoutedEventArgs e)
        {
            _shareScreenshotEnabled = ShareScreenshotCheckBox?.IsChecked == true;
            _leftScreenshotEnabled = LeftScreenshotCheckBox?.IsChecked == true;
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
