using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace R_Auto_Task.Helper
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
    }
}
