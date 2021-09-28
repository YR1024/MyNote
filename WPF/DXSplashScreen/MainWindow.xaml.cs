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

namespace DXSplashScreen
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

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            Thread t = LoadingThread();
            t.Start();
            GetData();
            t.Abort();
            //LoadingWnd loading = new LoadingWnd();
            //this.Dispatcher.Invoke(new Action(() =>
            //{
            //    loading.ShowDialog();
            //    GetData();
            //    loading.Close();
            //}));

        }

        public void GetData()
        {
            for (int i = 0; i < 11; i++)
            {
                Console.WriteLine(i); 
                System.Threading.Thread.Sleep(500);
            }
        }


        //Thread t = LoadingThread();
        //t.SetApartmentState(ApartmentState.STA);
        //        t.Start();

        Thread LoadingThread()
        {
            Thread t = new Thread(() =>
            {
                LoadingWnd loading = new LoadingWnd();
                loading.ShowInTaskbar = false;
                //loading.Owner = System.Windows.Application.Current.MainWindow;
                //this.Dispatcher.Invoke(new Action(() => { loading.Owner = this; }));
                loading.ShowDialog();
                System.Windows.Threading.Dispatcher.Run();
                loading.Closed += (d, k) =>
                {
                    System.Windows.Threading.Dispatcher.ExitAllFrames();
                };
            });
            t.SetApartmentState(ApartmentState.STA);
            return t;
        }
    }
}
