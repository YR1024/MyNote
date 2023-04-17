using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace LOLOnHookMonitor
{
    /// <summary>
    /// QQSpeedClose.xaml 的交互逻辑
    /// </summary>
    public partial class QQSpeedClose : Window
    {
        public QQSpeedClose()
        {
            InitializeComponent();
        }

        DateTime closeTime;
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            (sender as Button).IsEnabled = false;
            closeTime = DateTime.Now.Date.AddDays(1).AddMinutes(1);
            Task.Run(() => {

                while (true)
                {
                    if(DateTime.Now > closeTime)
                    {
                        Process[] GameAppProcesses = Process.GetProcessesByName("GameApp");
                        Process[] GameAppProcesses2 = Process.GetProcessesByName("勿念秒杀");
                        if (GameAppProcesses.Length >= 1)
                        {
                            GameAppProcesses[0].Kill();
                            if (GameAppProcesses2.Length >= 1)
                                GameAppProcesses2[0].Kill();
                            Process.GetCurrentProcess().Kill();
                        }
                    }
                    else
                    {
                        Thread.Sleep(5000);

                    }

                }
            });

            
        }
    }
}
