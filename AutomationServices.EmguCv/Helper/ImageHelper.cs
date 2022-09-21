using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Media.Imaging;

namespace AutomationServices.EmguCv.Helper
{
    public class ImageHelper
    {
        /// <summary>
        /// 将bitmap转换成base64字符串 
        /// </summary>
        /// <param name="bitmap">bitmap</param>
        /// <returns>base64字符串 </returns>
        public static string BitmapToString(Bitmap bitmap)
        {
            using (var memoryStream = new MemoryStream())
            {
                bitmap.Save(memoryStream, ImageFormat.Jpeg);
                byte[] byteImage = memoryStream.ToArray();
                // Get Base64
                return Convert.ToBase64String(byteImage);
            }
        }

        /// <summary>
        /// 将base64转换成bitmap图片
        /// </summary>
        /// <param name="base64String"></param>
        /// <returns>bitmap</returns>
        public static Bitmap StringToBitmap(string base64String)
        {
            Bitmap bmpReturn = null;
            //Convert Base64 string to byte[]
            byte[] byteBuffer = Convert.FromBase64String(base64String);
            using (var memoryStream = new MemoryStream(byteBuffer))
            {
                memoryStream.Position = 0;
                bmpReturn = (Bitmap)Bitmap.FromStream(memoryStream);
                return bmpReturn;
            }
        }

        /// <summary>
        /// Bitmap 转 BitmapImage
        /// </summary>
        /// <param name="bitmap"></param>
        /// <returns></returns>
        public static BitmapImage BitmapToImageSource(Bitmap bitmap)
        {
            using (var memory = new MemoryStream())
            {
                //这里需要将原来的bitmap重新复制一份出来使用newBitmap进行Save 否则会报GDI+中出现一般性错误
                //https://stackoverflow.com/questions/15862810/a-generic-error-occurred-in-gdi-in-bitmap-save-method
                Bitmap newBitmap = new Bitmap(bitmap);
                bitmap.Dispose();
                bitmap = null;

                newBitmap.Save(memory, System.Drawing.Imaging.ImageFormat.Bmp);
                memory.Position = 0;
                BitmapImage bitmapImage = new BitmapImage();
                bitmapImage.BeginInit();
                bitmapImage.StreamSource = memory;
                bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                bitmapImage.EndInit();

                return bitmapImage;
            }
        }

        /// <summary>
        /// 复制文件
        /// </summary>
        /// <param name="localFilePaht">要复制的文件路径</param>
        /// <param name="saveFilePath">指定存储的路径</param>
        public static void CopyAndSaveImage(string localFilePaht, string saveFilePath)
        {
           
            if (File.Exists(localFilePaht))//必须判断要复制的文件是否存在
            {
                if (!Directory.Exists("Image"))
                    Directory.CreateDirectory("Image");
                File.Copy(localFilePaht, saveFilePath, true);//三个参数分别是源文件路径，存储路径，若存储路径有相同文件是否替换
            }
        }

        /// <summary>
        /// 删除文件夹以及文件
        /// </summary>
        /// <param name="directoryPath"> 文件夹路径 </param>
        /// <param name="fileName"> 文件名称 </param>
        public static void DeleteImage(string directoryPath, string fileName)
        {
            if (fileName == null)
                return;

            //删除文件
            for (int i = 0; i < Directory.GetFiles(directoryPath).ToList().Count; i++)
            {
                if (Directory.GetFiles(directoryPath)[i] == fileName)
                {
                    File.Delete(fileName);
                }
            }

            ////删除文件夹
            //for (int i = 0; i < Directory.GetDirectories(directoryPath).ToList().Count; i++)
            //{
            //    if (Directory.GetDirectories(directoryPath)[i] == fileName)
            //    {
            //        Directory.Delete(fileName, true);
            //    }
            //}
        }

        /// <summary>
        /// 截取全屏幕图像
        /// </summary>
        /// <returns>屏幕位图</returns>
        public static Image GetFullScreen(string filename = default)
        {
            // take screenshot from primary display only
            //Image screen = Pranas.ScreenshotCapture.TakeScreenshot(true);
            //screen.Save(EmguCvHelper.FullScreenImage);
            //return screen;

            Image screen = new Bitmap(Screen.PrimaryScreen.Bounds.Width, Screen.PrimaryScreen.Bounds.Height);
            Graphics g = Graphics.FromImage(screen);
            g.CopyFromScreen(new Point(0, 0), new Point(0, 0), Screen.PrimaryScreen.Bounds.Size);
            if (filename != default)
            {
                //screen.Save(@".\文件名.jpg", ImageFormat.Jpeg);
                screen.Save(filename);
            }
            return screen;
        }

        /// <summary>
        /// 截取特定屏幕区域图像
        /// </summary>
        /// <returns>屏幕位图</returns>
        public static Image GetSpecificScreenArea(string filename, Rectangle RectArea)
        {
            //Image screen = new Bitmap(Screen.PrimaryScreen.Bounds.Width, Screen.PrimaryScreen.Bounds.Height);
            //Graphics g = Graphics.FromImage(screen);
            //g.CopyFromScreen(new Point(0, 0), new Point(0, 0), Screen.PrimaryScreen.Bounds.Size);
            //var PartialScreenImage = CaptureImage(screen, RectArea.X, RectArea.Y, RectArea.Width, RectArea.Height);
            //PartialScreenImage.Save(filename);
            //return screen;

            Image partialScreen = new Bitmap(RectArea.Width, RectArea.Height);
            Graphics g = Graphics.FromImage(partialScreen);
            g.CopyFromScreen(RectArea.X, RectArea.Y, 0, 0, RectArea.Size);

            partialScreen.Save(filename);
            //PartialScreenImage.Save(filename);
            return partialScreen;
        }

        /// <summary>
        /// 从大图中截取一部分图片
        /// </summary>
        /// <param name="fromImagePath">来源图片地址</param>        
        /// <param name="offsetX">从偏移X坐标位置开始截取</param>
        /// <param name="offsetY">从偏移Y坐标位置开始截取</param>
        /// <param name="width">保存图片的宽度</param>
        /// <param name="height">保存图片的高度</param>
        /// <returns></returns>
        public static System.Drawing.Image CaptureImage(System.Drawing.Image fromImage, int offsetX, int offsetY, int width, int height)
        {
            //创建新图位图
            Bitmap bitmap = new Bitmap(width, height);
            //创建作图区域
            Graphics graphic = Graphics.FromImage(bitmap);
            //截取原图相应区域写入作图区
            graphic.DrawImage(fromImage, 0, 0, new Rectangle(offsetX, offsetY, width, height), GraphicsUnit.Pixel);
            //从作图区生成新图
            System.Drawing.Image saveImage = System.Drawing.Image.FromHbitmap(bitmap.GetHbitmap());
            //保存图片
            //saveImage.Save(toImagePath, ImageFormat.Png);
            //释放资源   
            graphic.Dispose();
            bitmap.Dispose();
            //saveImage.Dispose();
            return saveImage;
        }

        /// <summary>
        /// 获取需要点击的位置坐标
        /// </summary>
        /// <param name="rect"></param>
        /// <param name="location"></param>
        /// <param name="offSet"></param>
        /// <returns></returns>
        public static Point GetClickPoint(Rectangle rect, MatchOptions MatOptions, ClickLocation location, Point offSet = default)
        {
            Point point;
            switch (location) 
            {
                case ClickLocation.Center: point = new Point(rect.X + rect.Width / 2, rect.Y + rect.Height / 2); break;

                case ClickLocation.LeftTop: point = new Point(rect.X, rect.Y); break;
                case ClickLocation.LeftCenter: point = new Point(rect.X, rect.Y + rect.Height / 2); break;
                case ClickLocation.LeftBottom: point = new Point(rect.X, rect.Y + rect.Height); break;

                case ClickLocation.CenterTop: point = new Point(rect.X + rect.Width / 2, rect.Y); break;
                case ClickLocation.CenterBottom: point = new Point(rect.X + rect.Width / 2, rect.Y + rect.Height); break;

                case ClickLocation.RightTop: point = new Point(rect.X + rect.Width, rect.Y); break;
                case ClickLocation.RightCenter: point = new Point(rect.X + rect.Width, rect.Y + rect.Height / 2); break;
                case ClickLocation.RightBottom: point = new Point(rect.X + rect.Width, rect.Y + rect.Height); break;

                default:return Point.Empty;
            }
            if(MatOptions.MatchMode == MatchMode.Relatively)
            {
                point.Offset(MatOptions.WindowArea.X, MatOptions.WindowArea.Y);
            }

            if (offSet != default)
            {
                point.Offset(offSet);
            }
            return point;
        }
    }
}
