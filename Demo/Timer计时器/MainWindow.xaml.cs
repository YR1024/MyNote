using System;
using System.Collections.Generic;
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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace WpfApp1
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            InitTimer();
        }

        Timer timerObj1;
        Timer timerObj2;
        int timesF = 0;
        int timesS = 0;

        void InitTimer()
        {
            timerObj1 = new Timer(new TimerCallback(TimerEvent1), null, Timeout.Infinite, 1000);
            timerObj2 = new Timer(new TimerCallback(TimerEvent2), null, Timeout.Infinite, 1000);
        }
      
        void TimerEvent1(object value)
        {

            this.times1.Dispatcher.Invoke(new Action(() => {
                times1.Text = (timesF++).ToString();
            }));
        }

        void TimerEvent2(object value)
        {
            this.times2.Dispatcher.Invoke(new Action(() => {
                times2.Text = (timesS++).ToString();
            }));
        }

        private void start(object sender, RoutedEventArgs e)
        {
            timerObj1.Change(0, 1000);//立即开始计时，时间间隔1000毫秒
            timerObj2.Change(0, 1000);//立即开始计时，时间间隔1000毫秒

        }
    }
}
