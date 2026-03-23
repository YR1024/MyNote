using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace ScheduleReminder
{
    /// <summary>
    /// NotifyWnd.xaml 的交互逻辑
    /// </summary>
    public partial class NotifyWnd : Window
    {
        #region 单例
        private static NotifyWnd _instance;
        public static NotifyWnd NewInstance
        {
            get
            {
                if (_instance != null)
                {
                    ManualShutdown();
                }

                _instance = new NotifyWnd();
                return _instance;
            }
        }
        private NotifyWnd()
        {
            InitializeComponent();
            Loaded += NotifyWindow_Loaded;
            closeTask = AutoCloseTask();
            Topmost = true;

            Height = 210;
            Width = 345;
            Left = SystemParameters.WorkArea.Right - Width;
            Top = SystemParameters.WorkArea.Bottom;
            animation = new DoubleAnimation
            {
                Duration = new Duration(TimeSpan.FromSeconds(0.5)),
                To = SystemParameters.WorkArea.Bottom - Height,
            };


            // 添加KeyDown事件处理
            KeyDown += (sender, e) =>
            {
                if (e.Key == Key.Escape)
                {
                    Close();
                }
            };
        }

        #endregion


        private DoubleAnimation animation;

        private void NotifyWindow_Loaded(object sender, RoutedEventArgs e)
        {
            BeginAnimation(TopProperty, animation);
        }

        int timing = 0;
        bool? cancelFlag = false;
        Task closeTask;
        Task AutoCloseTask()
        {
            return new Task(async () =>
            {
                if (AutoClose)
                {
                    timing = 0;
                    while (++timing < ShowTime * 10)
                    {
                        //source.Token.ThrowIfCancellationRequested();
                        if (cancelFlag == true)
                        {
                            cancelFlag = null;
                            return;
                        }
                        await Task.Delay(100);
                        Console.WriteLine(timing);
                    }
                    this.Dispatcher.Invoke(() =>
                    {
                        if (AutoClose)
                        {
                            Close();
                        }
                    });
                }
            });

        }


        public new void Show()
        {
            base.Show();
            timing = 0;
            //CloseTask.Start();
            closeTask.Start();
        }

        #region 属性

        private string message = "Message";
        public string Message
        {
            get { return message; }
            set
            {
                message = value;
                msg.Text = value;
            }
        }

        private bool autoClose = false;
        /// <summary>
        /// 是否自动关闭弹窗
        /// </summary>
        public bool AutoClose
        {
            get { return autoClose; }
            set { autoClose = value; }
        }

        private uint showTime = 10;
        /// <summary>
        /// 通知弹窗显示时间(秒)
        /// </summary>
        public uint ShowTime
        {
            get { return showTime; }
            set { showTime = value; }
        }
        #endregion


        private void Close_Click(object sender, RoutedEventArgs e)
        {
            if (_instance != null)
            {
                if (AutoClose)
                {
                    _instance.cancelFlag = true;
                    while (_instance.cancelFlag != null)
                    {
                        Thread.Sleep(10);
                    }
                }
                Close();
            }
        }

        private new void Close()
        {
            var animation = new DoubleAnimation
            {
                Duration = new Duration(TimeSpan.FromSeconds(0.3)),
                To = SystemParameters.WorkArea.Bottom,
            };
            animation.Completed += (ss, ee) => {
                this.Dispatcher.Invoke(() => {
                    _instance?.CloseWnd();
                    _instance = null;
                });
            };
            this.BeginAnimation(TopProperty, animation);
        }

        private void CloseWnd()
        {
            base.Close();
        }

        public static void ManualShutdown()
        {
            if (_instance != null)
            {
                if (_instance.AutoClose)
                {
                    _instance.cancelFlag = true; //取消自动关闭任务
                    while (_instance.cancelFlag != null) //等待任务完成
                    {
                        Thread.Sleep(10);
                    }
                }
                _instance.Dispatcher.Invoke(() => {
                    _instance?.Close();
                    _instance = null;
                });
            }
        }


    }
}
