using R_Auto_Task.Helper;
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

namespace R_Auto_Task
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
            TestTask();
        }

        void TestTask()
        {
            Task.Run(new Action(() =>
            {
                string pic = @"C:\Users\YR\Desktop\win.png";
                var rct = EmguCvHelper.GetMatchPos(pic);
                MouseHelper.MouseDownUp(rct.X + rct.Width / 2, rct.Y + rct.Height / 2);

                Thread.Sleep(1000);
                pic = @"C:\Users\YR\Desktop\qq.png";
                rct = EmguCvHelper.GetMatchPos(pic);
                MouseHelper.MouseDownUp(rct.X + rct.Width / 2, rct.Y + rct.Height / 2);

                Thread.Sleep(1000);
                pic = @"C:\Users\YR\Desktop\QQ2.png";
                rct = EmguCvHelper.GetMatchPos(pic);
                MouseHelper.MouseDownUp(rct.X + rct.Width / 2, rct.Y + rct.Height / 2);

                Thread.Sleep(3000);
                pic = @"C:\Users\YR\Desktop\qqhao.png";
                rct = EmguCvHelper.GetMatchPos(pic);
                MouseHelper.MouseDownUp(rct.X + rct.Width, rct.Y + rct.Height / 2);

                Thread.Sleep(1000);
                WinIoHelper.KeyDownUp(System.Windows.Forms.Keys.D1);
                Thread.Sleep(50);
                WinIoHelper.KeyDownUp(System.Windows.Forms.Keys.D9); 
                Thread.Sleep(50);
                WinIoHelper.KeyDownUp(System.Windows.Forms.Keys.D3);
                Thread.Sleep(50);
                WinIoHelper.KeyDownUp(System.Windows.Forms.Keys.D5); 
                Thread.Sleep(50);
                WinIoHelper.KeyDownUp(System.Windows.Forms.Keys.D8); 
                Thread.Sleep(50);
                WinIoHelper.KeyDownUp(System.Windows.Forms.Keys.D9); 
                Thread.Sleep(50);
                WinIoHelper.KeyDownUp(System.Windows.Forms.Keys.D3); 
                Thread.Sleep(50);
                WinIoHelper.KeyDownUp(System.Windows.Forms.Keys.D7);
                Thread.Sleep(50);
                WinIoHelper.KeyDownUp(System.Windows.Forms.Keys.D5); 
            }));
        }
    }
}
