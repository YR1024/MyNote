using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Services.Helper
{
    public class EmguCvHelper
    {

        public static string FullScreenImage = System.AppDomain.CurrentDomain.BaseDirectory + "FullScreenImage.png";
        public static string PartialScreenImage = System.AppDomain.CurrentDomain.BaseDirectory + "PartialScreenImage.png";


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
        /// <param name="img2"></param>
        /// <param name="Similarity"></param>
        /// <param name="threshold">相似度阈值，当匹配度达到</param>
        /// <returns></returns>
        public static Rectangle GetMatchPos(string img, out double Similarity, MatchOptions matchOptions)
        {
            Mat Src;
            if (matchOptions.MatchMode == MatchMode.Absolutely)
            {
                ImageHelper.GetFullScreen();
                Src = CvInvoke.Imread(FullScreenImage, matchOptions.ImreadModes);
            }
            else
            {
                ImageHelper.GetSpecificScreenArea(matchOptions.WindowArea);
                Src = CvInvoke.Imread(PartialScreenImage, matchOptions.ImreadModes);
            }

            //Test(img, out Similarity, matchOptions.Threshold);

            Mat Template = CvInvoke.Imread(img, matchOptions.ImreadModes);

            Mat MatchResult = new Mat();//匹配结果
            CvInvoke.MatchTemplate(Src, Template, MatchResult, Emgu.CV.CvEnum.TemplateMatchingType.CcorrNormed);//使用相关系数法匹配
            //CvInvoke.Threshold(MatchResult, MatchResult, 0.8, 1.0, ThresholdType.ToZero);
            Point max_loc = new Point();
            Point min_loc = new Point();
            double max = 0, min = 0;
            CvInvoke.MinMaxLoc(MatchResult, ref min, ref max, ref min_loc, ref max_loc);//获得极值信息

            Similarity = max;
            if (max > matchOptions.Threshold)
            {
                return new Rectangle(max_loc, Template.Size);
            }
            else
            {
                return Rectangle.Empty ;
            }
        }


        private static Rectangle Test(string img2, out double Similarity, double threshold = 0.98)
        {
            //ImageHelper.GetSpecificScreenArea(matchOptions.WindowArea);
            Mat Src = CvInvoke.Imread(PartialScreenImage, ImreadModes.Color);
            Mat Template = CvInvoke.Imread(img2, ImreadModes.Color);

            Mat MatchResult = new Mat();//匹配结果
            CvInvoke.MatchTemplate(Src, Template, MatchResult, Emgu.CV.CvEnum.TemplateMatchingType.CcorrNormed);//使用相关系数法匹配
            //CvInvoke.Threshold(MatchResult, MatchResult, 0.8, 1.0, ThresholdType.ToZero);
            Point max_loc = new Point();
            Point min_loc = new Point();
            double max = 0, min = 0;
            CvInvoke.MinMaxLoc(MatchResult, ref min, ref max, ref min_loc, ref max_loc);//获得极值信息
            Similarity = max;
            if (max > threshold)
            {
                return new Rectangle(max_loc, Template.Size);
            }
            else
            {
                return Rectangle.Empty;
            }
        }
    }
}
