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

namespace LoadingWnd
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        void aa()
        {
            System.Windows.Application.Current.Dispatcher.Invoke((Action)(() =>
            {
               
            }));
        }

        int i = 0;
        private  void Button_Click(object sender, RoutedEventArgs e)
        {
            i = 0;
       
            Window t = ShowLoadingWindow();

            List<int> array = new List<int>();
            for (int j = 0; j < 5; j++)
            {
                array.Add(1);
                Thread.Sleep(1000);
            }

            (t as LoadWindow).CloseLoading();

            //Task t2 = new Task(async () =>
            //{

            //    while (true)
            //    {
            //        i++;
            //        if (i>5)
            //        {
            //            (t as LoadWindow).CloseLoading();
            //            break;
            //        }
            //        else
            //        {
            //            await Task.Delay(1000);
            //        }
            //    }
            //});
            //t2.Start();
        }

        Window ShowLoadingWindow()
        {
            LoadWindow load = new LoadWindow();
            load.Owner = this;
            load.ShowInTaskbar = false;
            Task t = new Task(() =>
            {
           
                while (true)
                {
                    
                    if (load.closeFlag)
                    {
                        System.Windows.Application.Current.Dispatcher.Invoke((Action)(() =>
                        {
                            load.DialogResult = true;
                        }));
                        break;
                    }
                    else
                    {
                        Thread.Sleep(100);
                    }
                }
            });
            Task t2 = new Task(() =>
            {
                System.Windows.Application.Current.Dispatcher.Invoke((Action)(() =>
                {
                    load.ShowDialog();
                }));
            });
            t2.Start();
            t.Start();
            return load;
        }

    }
}
