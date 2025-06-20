using SharpDX.XInput;
using SimWinInput;
using System;
using System.ComponentModel;
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
        Controller controller = new Controller(UserIndex.One);
        bool lastButtonState = false;

        public MainWindow()
        {
            //ImageRecognition.Test();
            InitializeComponent();
            SimGamePad.Instance.Initialize(); //
            SimGamePad.Instance.PlugIn();
            Closing += MainWindow_Closing;


            // 启动手柄监听
            StartGamepadHotkeyListener();
        }

        private void MainWindow_Closing(object sender, CancelEventArgs e)
        {
            Hotkey.UnRegist(new WindowInteropHelper(this).Handle, HotKeyPressd);
            Hotkey.UnRegist(new WindowInteropHelper(this).Handle, GamePadHotKey);
            //UnregisterHotkey();
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
            });
        }


        bool startLoop = false;
        bool hasStoped = true;
        int loopCount = 0;
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
                    this.Dispatcher.Invoke(() => {
                        count.Text = $"{loopCount}";
                    });
                }
                hasStoped = true;
            });
        }



        public void GamePadHotKey()
        {
            SimGamePad.Instance.Use(GamePadControl.A, 0, 3000);
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
                Task.Run(() => {
                    while (!hasStoped)
                    {
                        Thread.Sleep(5);
                    }
                    this.Dispatcher.Invoke(() => {
                        info.Text = "暂停";
                        count.Text = "";
                    });

                });

            }

        }


        #region 快捷键

        protected override void OnSourceInitialized(EventArgs e)
        {
            Hotkey.Regist(this, HotkeyModifiers.MOD_CONTROL, Key.OemQuestion, HotKeyPressd);
            Hotkey.Regist(this, HotkeyModifiers.MOD_CONTROL, Key.M, GamePadHotKey);
            //Hotkey.UnRegist(new WindowInteropHelper(this).Handle, () =>
            //{
            //    Console.WriteLine("取消快捷键");
            //    System.Windows.MessageBox.Show("取消快捷键");

            //});
        }

        //private GlobalHotkey hotkey;

        //public void RegisterHotkey()
        //{
        //    hotkey = new GlobalHotkey(new WindowInteropHelper(this).Handle, 1);
        //    hotkey.HotkeyPressed += Hotkey_HotkeyPressed;
        //}

        //private void Hotkey_HotkeyPressed(object sender, HotkeyEventArgs e)
        //{
        //    // 处理全局热键按下事件
        //    if (e.Modifiers == ModifierKeys.Control && e.Key == Key.F12)
        //    {
        //        // 在这里执行你的自定义操作
        //        System.Windows.MessageBox.Show("全局热键 Ctrl + F12 被按下了");
        //    }
        //}

        //private void UnregisterHotkey()
        //{
        //    hotkey.UnregisterHotkey();
        //}
        #endregion
    }
}
