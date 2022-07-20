using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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

namespace Quickspot
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

        CompareResult compareResult = null;

        private void Compare_Click(object sender, RoutedEventArgs e)
        {
            compareResult?.CloseWindow();

            ImageParse.Init();
            Task.Run(() =>
            {
                ImageParse.LoadImage1();
                ImageParse.LoadImage2();
            });

            while (true)
            {
                if (ImageParse.IsImage1Loaded && ImageParse.IsImage2Loaded)
                {
                    break;
                }
            }

            Task.Run(() =>
            {
                ImageParse.Compare();
            });

            while (true)
            {
                if (ImageParse.Result.Count == ImageParse.sourceImg.Count)
                {
                    break;
                }
            }


            compareResult = new CompareResult(ImageParse.Result);
            compareResult.ShowCompareResult();

        }

        private void Clear_Click(object sender, RoutedEventArgs e)
        {
            compareResult?.CloseWindow();
            //ImageParse.Init();
        }
    }
}
