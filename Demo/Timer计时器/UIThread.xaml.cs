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
    /// UIThread.xaml 的交互逻辑
    /// </summary>
    public partial class UIThread : UserControl
    {
        public UIThread()
        {
            InitializeComponent();
            timerObj1 = new Timer(new TimerCallback(TimerEvent1), null, Timeout.Infinite, 1000);
        }


        Timer timerObj1;
        int times = 0;
        List<ClassA> CAList = new List<ClassA>();

        bool IsExcutEnd = false;
        void TimerEvent1(object value)
        {
            if(IsExcutEnd == true)
            {
                timerObj1.Change(Timeout.Infinite, Timeout.Infinite);
                return;
            }
            this.funtimes.Dispatcher.Invoke(new Action(() => {
                 funtimes.Text = (times++).ToString();
            }));
            System.Windows.Application.Current.Dispatcher.Invoke(new Action(() =>
            {
                for (int i = 0; i < 10; i++)
                {
                    CAList.Add(new ClassA());
                }
            }));
            this.InputBox.Dispatcher.Invoke(new Action(() =>
            {
                InputBox.Text = (CAList.Count).ToString();
            }));
            IsExcutEnd = true;
        }


        private void Button_Click(object sender, RoutedEventArgs e)
        {
        
            timerObj1.Change(0, 1000);
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            timerObj1.Change(Timeout.Infinite, Timeout.Infinite);
        }
    }

    public class ClassA
    {
        public ClassA()
        {
            for (int i = 0; i < 10_0000; i++)
            {
                Random rd = new Random();
                num[i] = rd.Next(0, 10000);
            }
        }
        int[] num = new int[10_0000];
    }
}
