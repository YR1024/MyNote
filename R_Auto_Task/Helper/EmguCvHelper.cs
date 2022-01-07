using Emgu.CV;
using Emgu.CV.CvEnum;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace R_Auto_Task.Helper
{
    public class EmguCvHelper
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="img1">大图</param>
        /// <param name="img2">小图</param>
        /// <returns></returns>
        public static Rectangle GetMatchPos(string img1, string img2)
        {
            Mat Src = CvInvoke.Imread(img1, ImreadModes.Grayscale);
            Mat Template = CvInvoke.Imread(img2, ImreadModes.Grayscale);

            Mat MatchResult = new Mat();//匹配结果
            CvInvoke.MatchTemplate(Src, Template, MatchResult, Emgu.CV.CvEnum.TemplateMatchingType.CcorrNormed);//使用相关系数法匹配
            Point max_loc = new Point();
            Point min_loc = new Point();
            double max = 0, min = 0;
            CvInvoke.MinMaxLoc(MatchResult, ref min, ref max, ref min_loc, ref max_loc);//获得极值信息

            return new Rectangle(max_loc, Template.Size);
        }

        /// <summary>
        /// 在屏幕中寻找图片的位置
        /// </summary>
        /// <param name="img2">小图</param>
        /// <returns></returns>
        public static Rectangle GetMatchPos(string img2)
        {
            GetFullScreen();
            Mat Src = CvInvoke.Imread(FullScreenImage, ImreadModes.Grayscale);
            Mat Template = CvInvoke.Imread(img2, ImreadModes.Grayscale);

            Mat MatchResult = new Mat();//匹配结果
            CvInvoke.MatchTemplate(Src, Template, MatchResult, Emgu.CV.CvEnum.TemplateMatchingType.CcorrNormed);//使用相关系数法匹配
            Point max_loc = new Point();
            Point min_loc = new Point();
            double max = 0, min = 0;
            CvInvoke.MinMaxLoc(MatchResult, ref min, ref max, ref min_loc, ref max_loc);//获得极值信息

            return new Rectangle(max_loc, Template.Size);
        }


        public static string FullScreenImage = @"C:\Users\YR\Desktop\FullScreenImage.png";




        /// <summary>
        /// 截取全屏幕图像
        /// </summary>
        /// <returns>屏幕位图</returns>
        public static Image GetFullScreen()
        {
            //Bitmap mimage = new Bitmap(System.Windows.Forms.Screen.PrimaryScreen.Bounds.Width, System.Windows.Forms.Screen.PrimaryScreen.Bounds.Height);
            //Graphics gp = Graphics.FromImage(mimage);
            //gp.CopyFromScreen(new Point(System.Windows.Forms.Screen.PrimaryScreen.Bounds.X, System.Windows.Forms.Screen.PrimaryScreen.Bounds.Y), new Point(0, 0), mimage.Size, CopyPixelOperation.SourceCopy);
            //mimage.Save(FullScreenImage);
            //return mimage;

            // take screenshot from primary display only
            Image screen = Pranas.ScreenshotCapture.TakeScreenshot(true);
            screen.Save(FullScreenImage);
            return screen;
        }
    }
}
