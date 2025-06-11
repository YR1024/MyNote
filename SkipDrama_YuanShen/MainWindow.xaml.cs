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
        public MainWindow()
        {
            //ImageRecognition.Test();
            InitializeComponent();
            SimGamePad.Instance.Initialize(); //
            SimGamePad.Instance.PlugIn();
            Closing += MainWindow_Closing;

        }

        private void MainWindow_Closing(object sender, CancelEventArgs e)
        {
            Hotkey.UnRegist(new WindowInteropHelper(this).Handle, HotKeyPressd);
            //UnregisterHotkey();
            SimGamePad.Instance.Unplug();
            SimGamePad.Instance.ShutDown();
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
