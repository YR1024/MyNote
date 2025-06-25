using HidSharp;
using SharpDX.XInput;
using SimWinInput;
using System;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;

namespace SkipDrama_YuanShen
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {

        // 在 MainWindow 类中添加字段
        State previousState = new State();
        State currentState = new State();
        Controller controller = new Controller(UserIndex.One);

        public MainWindow()
        {
            InitializeComponent();

            SimGamePad.Instance.Initialize(); //
            SimGamePad.Instance.PlugIn();
            StartGamepadHotkeyListener();

            Loaded += MainWindow_Loaded;
            // 启动手柄监听
            Closing += MainWindow_Closing;
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            ListenXboxControllerByHID();
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
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
        /// 监听手柄A键长按3秒
        /// </summary>
        private void StartGamepadHotkeyListener()
        {
            //return;
            Task.Run(() =>
            {
                bool isAPressed = false;
                DateTime aPressedStart = DateTime.MinValue;

                while (true)
                {
                    if (controller.IsConnected)
                    {
                        var state = controller.GetState();
                        bool currentAPressed = (state.Gamepad.Buttons & GamepadButtonFlags.A) != 0;

                        if (currentAPressed)
                        {
                            if (!isAPressed)
                            {
                                // 第一次按下A键，记录时间
                                aPressedStart = DateTime.Now;
                                isAPressed = true;
                            }
                            else
                            {
                                // 已经按下，判断是否超过3秒
                                if ((DateTime.Now - aPressedStart).TotalSeconds >= 3)
                                {
                                    // 只触发一次
                                    this.Dispatcher.Invoke(() => HotKeyPressd());
                                    // 避免重复触发，直到松开再允许
                                    isAPressed = false;
                                }
                            }
                        }
                        else
                        {
                            // 松开A键，重置状态
                            isAPressed = false;
                        }
                    }
                    Thread.Sleep(50); // 轮询间隔
                }



                {
                    /*
                    // 获取控制器状态
                    controller.GetState(out currentState);

                    // 检查按钮状态
                    if (previousState.Gamepad.Buttons != currentState.Gamepad.Buttons)
                    {
                        // 按键状态发生了改变
                        if (currentState.Gamepad.Buttons.HasFlag(GamepadButtonFlags.A))
                        {
                            // A 按钮按下
                            Console.WriteLine("A 按钮按下");
                        }
                        else if (currentState.Gamepad.Buttons.HasFlag(GamepadButtonFlags.A) && !previousState.Gamepad.Buttons.HasFlag(GamepadButtonFlags.A))
                        {
                            // A 按钮按住不放
                            Console.WriteLine("A 按钮按住不放");
                        }
                        else if (!currentState.Gamepad.Buttons.HasFlag(GamepadButtonFlags.A) && previousState.Gamepad.Buttons.HasFlag(GamepadButtonFlags.A))
                        {
                            // A 按钮释放
                            Console.WriteLine("A 按钮释放");
                        }

                        // 更新上一次的状态
                        previousState = currentState;
                    }
                    */
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


        #region HID 方式监听手柄
        void ListenXboxControllerByHID()
        {
            // 1. 查找 Xbox 手柄设备
            var devices = DeviceList.Local.GetHidDevices();
            var xboxController = devices.FirstOrDefault(d =>
                d.VendorID == 0x045E && d.ProductID == 0x0B13 // 0x0B13为Xbox Series手柄，其他型号请查实际PID
            );

            if (xboxController == null)
            {
                Console.WriteLine("未找到Xbox手柄。");
                return;
            }

            // 2. 打开输入流
            using (var stream = xboxController.Open())
            {
                byte[] buffer = new byte[xboxController.MaxInputReportLength];
                Console.WriteLine("开始监听手柄输入...");
                while (true)
                {
                    int count = stream.Read(buffer, 0, buffer.Length);
                    if (count > 0)
                    {
                        // 3. 解析报文，查找“分享键”状态
                        bool sharePressed = IsShareButtonPressed(buffer);
                        if (sharePressed)
                        {
                            Console.WriteLine("分享键被按下！");
                        }
                    }
                }
            }
        }

        // 解析HID报文，判断“分享键”状态
        static bool IsShareButtonPressed(byte[] report)
        {
            // 以Xbox Series手柄为例，USB有线模式下
            // 通常第4字节的bit4为“分享键”
            // 具体bit位置请用HID工具抓包确认
            // 例如: report[4] & 0x10 != 0
            if (report.Length > 4)
            {
                return (report[4] & 0x10) != 0;
            }
            return false;
        }
        #endregion

    }
}
