using Device.Net;
using Hid.Net.Windows;
using Microsoft.Extensions.Logging;
using SharpDX.XInput;
using SimWinInput;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using WpfApp2;


namespace SkipDrama_YuanShen
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {

        // 在 MainWindow 类中添加字段
        Controller controller = new Controller(UserIndex.One);

        public MainWindow()
        {
            InitializeComponent();

            SimGamePad.Instance.Initialize(); //
            SimGamePad.Instance.PlugIn();
            StartGamepadHotkeyListener2();
            Loaded += MainWindow_Loaded;
            // 启动手柄监听
            Closing += MainWindow_Closing;
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            //ListenGamepadHID();
            // 注册热键
            Hotkey.Regist(this, HotkeyModifiers.MOD_CONTROL, Key.OemQuestion, HotKeyPressd);
        }

        private void MainWindow_Closing(object sender, CancelEventArgs e)
        {
            // 注销热键
            Hotkey.UnRegist(new WindowInteropHelper(this).Handle, HotKeyPressd);
            SimGamePad.Instance.Unplug();//拔下虚拟 GamePad
            SimGamePad.Instance.ShutDown();
        }


        /// <summary>
        /// 监听手柄左扳机 + X 键组合（按住超过 0.5 秒触发一次）
        /// </summary>
        private void StartGamepadHotkeyListener2()
        {
            Task.Run(() =>
            {
                bool isComboPressed = false;    // 当前是否处于“按下”状态（用于记录按下时刻）
                bool hasTriggered = false;      // 本次按住是否已触发过事件（触发后必须松开才能再次触发）

                bool isLeftPressed = false;      // 左键是否处于“按下”状态（用于记录按下时刻）
                bool lhasTriggered = false;      // 本次按住是否已触发过事件（触发后必须松开才能再次触发）
                DateTime comboPressedStart = DateTime.MinValue;
                DateTime leftPressedStart = DateTime.MinValue;

                const byte leftTriggerThreshold = 30; // 阈值，可按需调整 (0-255)
                const double holdSecondsToTrigger = 0.05; // 持续时间阈值

                while (true)
                {
                    try
                    {
                        if (controller.IsConnected)
                        {
                            var state = controller.GetState();

                            bool leftTriggerActive = state.Gamepad.LeftTrigger > leftTriggerThreshold;
                            bool xButtonPressed = (state.Gamepad.Buttons & GamepadButtonFlags.X) != 0;

                            bool currentCombo = leftTriggerActive && xButtonPressed;
                           

                            if (currentCombo)
                            {
                                if (!isComboPressed)
                                {
                                    // 第一次检测到组合按下，记录时间
                                    comboPressedStart = DateTime.Now;
                                    isComboPressed = true;
                                }
                                else if (!hasTriggered)
                                {
                                    // 已处于按住状态且尚未触发过，判断是否超过阈值时长
                                    if ((DateTime.Now - comboPressedStart).TotalSeconds >= holdSecondsToTrigger)
                                    {
                                        // 触发一次，并标记已触发，直到松开任意键才允许下一次触发
                                        this.Dispatcher.Invoke(() => HotKeyPressd());
                                        hasTriggered = true;
                                    }
                                }
                                // 如果 hasTriggered 已为 true，则保持等待释放，不重复触发
                            }
                            else
                            {
                                // 组合松开（任意一个键松开），重置状态，允许下一次触发
                                isComboPressed = false;
                                hasTriggered = false;
                            }


                            #region 左键截图
                            bool lButtonPressed = (state.Gamepad.Buttons & GamepadButtonFlags.DPadLeft) != 0;
                            bool isScreenshot = false;
                            this.Dispatcher.Invoke(() => {
                                isScreenshot = screenshotChkBox.IsChecked.Value;
                            });
                            if (lButtonPressed && isScreenshot)
                            {
                                if (!isLeftPressed)
                                {
                                    // 第一次检测到组合按下，记录时间
                                    leftPressedStart = DateTime.Now;
                                    isLeftPressed = true;
                                }
                                else if (!lhasTriggered)
                                {
                                    // 已处于按住状态且尚未触发过，判断是否超过阈值时长
                                    if ((DateTime.Now - leftPressedStart).TotalSeconds >= holdSecondsToTrigger)
                                    {
                                        // 触发一次，并标记已触发，直到松开任意键才允许下一次触发
                                        //SimKeyboard.KeyDown(18); //alt
                                        //SimKeyboard.KeyDown(112);  //f1
                                        //SimKeyboard.KeyUp(112);
                                        //SimKeyboard.KeyUp(18);
                                        ScreenshotHelper.Screenshot();
                                        lhasTriggered = true;
                                    }
                                }
                            }
                            else
                            {
                                // 组合松开（任意一个键松开），重置状态，允许下一次触发
                                isLeftPressed = false;
                                lhasTriggered = false;
                            }

                            #endregion

                        }
                    }
                    catch
                    {
                        // 忽略临时读取异常，继续轮询
                    }

                    Thread.Sleep(5); // 轮询间隔
                }

            });
        }


        bool startLoop = false;
        bool hasStoped = true;
        int loopCount = 0;
        /// <summary>
        /// 循环点击 手柄 A键
        /// </summary>
        void LoopClickGamePadTask()
        {
            startLoop = true;
            loopCount = 0;
            Task.Run(() =>
            {
                //Thread.Sleep(1000);
                //SimMouse.Click(MouseButtons.Left, 37, 138);
                //Console.WriteLine("Click");

                while (startLoop)
                {
                    hasStoped = false;
                    SimGamePad.Instance.Use(GamePadControl.A, 0, 50);
                    Thread.Sleep(30);

                    //// Pull and release the left trigger.
                    //SimGamePad.Instance.Use(GamePadControl.LeftTrigger);
                    //// Move the right analog stick into the leftmost position, then return to neutral position.
                    //SimGamePad.Instance.Use(GamePadControl.RightStickLeft);

                    loopCount++;
                    this.Dispatcher.Invoke(() =>
                    {
                        count.Text = $"{loopCount}";
                    });
                }
                hasStoped = true;
            });
        }


        /// <summary>
        /// 热键（快捷键）触发事件
        /// </summary>
        public void HotKeyPressd()
        {
            if (hasStoped)
            {
                LoopClickGamePadTask();
                info.Text = "A";
            }
            else
            {
                startLoop = false;
                Task.Run(() =>
                {
                    while (!hasStoped)
                    {
                        Thread.Sleep(5);
                    }
                    this.Dispatcher.Invoke(() =>
                    {
                        info.Text = "暂停";
                        count.Text = "";
                    });

                });

            }

        }

















        #region HidLibrary
        private IDevice _gamepadDevice;

        private async void ListenGamepadHID()
        {
            // 替换为您的手柄 VendorId 和 ProductId
            var filter = new FilterDeviceDefinition(vendorId: 0x045E, productId: 0x02FF, usagePage: 0x01, label: "Xbox Controller");
            var factory = filter.CreateWindowsHidDeviceFactory(loggerFactory: null, writeBufferSize: 100);

            var devices = (await factory.GetConnectedDeviceDefinitionsAsync()).ToList();
            if (devices.Count == 0)
            {
                MessageBox.Show("未找到手柄设备");
                return;
            }

            _gamepadDevice = await factory.GetDeviceAsync(devices.First());
            await _gamepadDevice.InitializeAsync();

            // 持续读取数据
            _ = Task.Run(async () =>
            {
                while (true)
                {
                    var readResult = await _gamepadDevice.ReadAsync();
                    if (readResult.Data != null && readResult.Data.Length > 0)
                    {
                        // 解析按键数据（具体格式需参考手柄 HID 报告描述符）
                        Dispatcher.Invoke(() =>
                        {
                            // 示例：显示原始数据
                            info.Text = BitConverter.ToString(readResult.Data);
                        });
                    }
                    await Task.Delay(10);
                }
            });
        }

        //private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        //{
        //    ListenGamepadHID();
        //}

        #endregion


        #region RawInputHid
        //private RawInputHidListener _listener;

        //protected override void OnSourceInitialized(EventArgs e)
        //{
        //    base.OnSourceInitialized(e);

        //    var hwnd = new WindowInteropHelper(this).Handle;

        //    _listener = new RawInputHidListener(hwnd);
        //    _listener.HidReport += OnHidReport;

        //    _listener.Start();
        //    Debug.WriteLine("RawInput HID listener started.");
        //}

        //private void OnHidReport(object sender, HidReportEventArgs e)
        //{
        //    // 这里就是你后续解析 Share 键/按键位的地方
        //    // 你现在可以先只打印报文（不要在这里做耗时工作）
        //    Debug.WriteLine($"HID Report: bytes={e.Report.Length}, data={BitConverter.ToString(e.Report)}");

        //    // 示例：如果你要判断某个 bit，就在这里做：
        //    if (e.Report.Length > 4 && (e.Report[12] & 0x08) != 0) 
        //    {
        //        //SimKeyboard.Press((byte)'q');
        //        SimKeyboard.KeyDown(18); //alt
        //        SimKeyboard.KeyDown(112);  //f1
        //        SimKeyboard.KeyUp(112);
        //        SimKeyboard.KeyUp(18);
        //    }
        //}
        ////HID Report: bytes=16, data=00-43-85-84-80-05-7D-66-86-00-80-00-08-00-00-00 //Share键按下
        ////HID Report: bytes=16, data=00-43-85-84-80-05-7D-66-86-00-80-00-00-00-00-00
        ////HID Report: bytes=16, data=00-43-85-84-80-05-7D-66-86-00-80-00-04-00-00-00 //西瓜键按下
        ////HID Report: bytes=16, data=00-43-85-84-80-05-7D-66-86-00-80-00-00-00-00-00
        //protected override void OnClosed(EventArgs e)
        //{
        //    base.OnClosed(e);
        //    if (_listener != null)
        //    {
        //        _listener.Dispose();
        //        _listener = null;
        //    }
        //}
        #endregion

    }
}
