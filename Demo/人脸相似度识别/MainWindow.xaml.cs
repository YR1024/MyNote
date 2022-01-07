using Microsoft.Win32;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
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

namespace 人脸相似度识别
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

        public string api_key = "CSlcv0p7FHdfv7dNXbX8mg7V";
        public string secret_key = "mQEpA12q2L3Xc4DtdOhGd1KFuFuBWo2a";

        public string img1 = "";
        public string img2 = "";

        private void Border_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.InitialDirectory = "c:\\";//注意这里写路径时要用c:\\而不是c:\
            openFileDialog.Filter = "图像文件|*.png;*.jpg;*.bmp";
            openFileDialog.RestoreDirectory = true;
            openFileDialog.FilterIndex = 1;
            if (openFileDialog.ShowDialog() == true)
            {
                img1 = openFileDialog.FileName;
                image1.Source = FileToImageSource(img1);
            }

        }

        private void Border2_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.InitialDirectory = "c:\\";//注意这里写路径时要用c:\\而不是c:\
            openFileDialog.Filter = "图像文件|*.png;*.jpg;*.bmp";
            openFileDialog.RestoreDirectory = true;
            openFileDialog.FilterIndex = 1;
            if (openFileDialog.ShowDialog() == true)
            {
                img2 = openFileDialog.FileName;
                image2.Source = FileToImageSource(img2);
            }

        }

        ImageSource FileToImageSource(string file)
        {
            System.Drawing.Bitmap bitmap = new System.Drawing.Bitmap(file);

            MemoryStream stream = new MemoryStream();

            bitmap.Save(stream, System.Drawing.Imaging.ImageFormat.Png);

            ImageBrush imageBrush = new ImageBrush();

            ImageSourceConverter imageSourceConverter = new ImageSourceConverter();

            return (ImageSource)imageSourceConverter.ConvertFrom(stream);

        }


        void matchingFace()
        {
            if (image2.Source == null || image1.Source == null)
            {
                MessageBox.Show("请先复制图片到图片框");
                return;
            }

            Baidu.Aip.Face.Face client = new Baidu.Aip.Face.Face(api_key, secret_key);
            List<byte[]> list = new List<byte[]>();


            list.Add(ImageToByte((Bitmap)(new System.Drawing.Bitmap(img1))));
            list.Add(ImageToByte((Bitmap)(new System.Drawing.Bitmap(img2))));
            JArray ja = new JArray(list.ToArray());
            JObject result = client.Match(ja);
            if ((int)result["result_num"] == 0)
            {
                textBlock.Text = "匹配失败";
            }
            else
            {
                JArray jarr = (JArray)result["result"];
                string score = jarr[0]["score"].ToString();
                textBlock.Text = "匹配度：" + score;
            }
        }


        //图片转byte[]
        public byte[] ImageToByte(Bitmap inImg)
        {
            MemoryStream mstream = new MemoryStream();
            inImg.Save(mstream, ImageFormat.Bmp);
            byte[] bytes = new Byte[mstream.Length];
            mstream.Position = 0;
            mstream.Read(bytes, 0, bytes.Length);
            mstream.Close();
            return bytes;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            matchingFace();
        }
    }
}
