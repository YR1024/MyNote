using AutomationServices.EmguCv;
using AutomationServices.EmguCv.Helper;
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
using System.Windows.Shapes;

namespace QQSpeed_SmartApp
{
    /// <summary>
    /// Window1.xaml 的交互逻辑
    /// </summary>
    public partial class Window1 : Window
    {
        public static string BaseDirectory = System.AppDomain.CurrentDomain.BaseDirectory + "Images\\";

        public Window1()
        {
            InitializeComponent();
        }

        private void Start(object sender, RoutedEventArgs e)
        {
            (sender as Button).Content = "运行中";
            (sender as Button).IsEnabled = false;

            Task.Run(() =>
            {
                string image1 = BaseDirectory + "对战币.png";
                string image2 = BaseDirectory + "开始匹配.png";
                string image3 = BaseDirectory + "确定.png";
                var matchOptions = new MatchOptions();
                matchOptions.MaxTimes = 0;
                matchOptions.DelayInterval = 2000;
                matchOptions.Threshold = 0.97;
                matchOptions.MatchMode = MatchMode.Absolutely;
                //matchOptions.WindowArea = WindowHelper.GetWindowLocationSize(QQSpeedProcess.MainWindowHandle);
                matchOptions.ImreadModesConvert = ImreadModesConvert.Color;

                while (true)
                {
                    //开始匹配
                    MyHelper.WaitFindAndClick(image2, default, default, matchOptions); 
                    //使用天梯劵
                    MyHelper.WaitFindAndClick(image3, default, default, matchOptions); 
                    //等待完成
                    MyHelper.WaitFindAndClick(image1, default, new System.Drawing.Point(85, 120), matchOptions);
                    Thread.Sleep(500);
                }
            });
          
        }
    }
}
