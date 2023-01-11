using AutomationServices.EmguCv.Helper;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
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

namespace TestDemo
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            //Test();

            //OpenCvSharpHelper.SplitDigits(AppDomain.CurrentDomain.BaseDirectory + "digits.png");

            //OpenCvSharpHelper.Train();
            OpenCvSharpHelper.LoadKnnAndPredict();
        }

        



        void Test()
        {
            Bitmap src = new Bitmap(@"F:\dance\719.png");
            //Bitmap sub = new Bitmap(@"F:\dance\Right.png");
            //Bitmap sub = new Bitmap(@"F:\dance\Up.png");
            //Bitmap sub = new Bitmap(@"F:\dance\Down.png");
            Bitmap sub = new Bitmap(@"F:\dance\Left.png");
            var bitmap = OpenCvSharpHelper.MatchPicBySurf(src, sub, 1500);

            ImageBox.Source = BitmapSourceConvert.ToBitmapSource(bitmap);
        }
    }

    public static class BitmapSourceConvert
    {


        public static BitmapImage ToBitmapSource(Bitmap b)
        {
            //Bitmap b = new Bitmap(bCode);
            MemoryStream ms = new MemoryStream();
            b.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
            byte[] bytes = ms.GetBuffer();  //byte[]   bytes=   ms.ToArray(); 这两句都可以
            ms.Close();
            //Convert it to BitmapImage
            BitmapImage image = new BitmapImage();
            image.BeginInit();
            image.StreamSource = new MemoryStream(bytes);
            image.EndInit();
            return image;
        }
    }
}
