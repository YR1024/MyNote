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


        /// <summary>
        /// 
        /// </summary>
        /// <param name="localFilePaht">要复制的文件路径</param>
        /// <param name="saveFilePath">指定存储的路径</param>
        public static void SaveImage(string localFilePaht, string saveFilePath)
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

    }
}
