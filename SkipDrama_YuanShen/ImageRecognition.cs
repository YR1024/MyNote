using AForge.Imaging;
using Emgu.CV;
using Emgu.CV.CvEnum;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SkipDrama_YuanShen
{
    public class ImageRecognition
    {

        public static void Test()
        {
            // 加载模板图片
            Bitmap templateBitmap = new Bitmap(@"D:\360MoveData\Users\YR\Desktop\Test.png");

            // 确保模板图片是24位RGB格式
            Bitmap templateBitmap24bpp = ConvertTo24bpp(templateBitmap);

            // 截取屏幕
            Bitmap screenBitmap = CaptureScreen();

            // 确保屏幕截图是24位RGB格式
            Bitmap screenBitmap24bpp = ConvertTo24bpp(screenBitmap);

            // 使用模板匹配查找模板图片在屏幕中的位置
            TemplateMatch[] matches = FindTemplate(screenBitmap24bpp, templateBitmap24bpp);

            if (matches.Length > 0)
            {
                Console.WriteLine("模板图片找到，位置：");
                foreach (var match in matches)
                {
                    Console.WriteLine($"X: {match.Rectangle.X}, Y: {match.Rectangle.Y}");
                }
            }
            else
            {
                Console.WriteLine("模板图片未找到");
            }

            // 释放图像资源
            templateBitmap.Dispose();
            templateBitmap24bpp.Dispose();
            screenBitmap.Dispose();
            screenBitmap24bpp.Dispose();
        }

        static Bitmap CaptureScreen()
        {
            Rectangle screenSize = System.Windows.Forms.Screen.PrimaryScreen.Bounds;
            Bitmap bitmap = new Bitmap(screenSize.Width, screenSize.Height);
            using (Graphics g = Graphics.FromImage(bitmap))
            {
                g.CopyFromScreen(screenSize.Left, screenSize.Top, 0, 0, screenSize.Size);
            }
            return bitmap;
        }

        static Bitmap ConvertTo24bpp(Bitmap img)
        {
            Bitmap newImage = new Bitmap(img.Width, img.Height, PixelFormat.Format24bppRgb);
            using (Graphics gr = Graphics.FromImage(newImage))
            {
                gr.DrawImage(img, new Rectangle(0, 0, img.Width, img.Height));
            }
            return newImage;
        }

        static TemplateMatch[] FindTemplate(Bitmap source, Bitmap template)
        {
            ExhaustiveTemplateMatching tm = new ExhaustiveTemplateMatching(0.9f); // 设置相似度阈值
            TemplateMatch[] matches = tm.ProcessImage(source, template);
            return matches;
        }


       

    }



    public class EmguCVImageRecognition
    {
        public static void Test()
        {
            // 加载模板图片
            Mat template = CvInvoke.Imread(@"D:\360MoveData\Users\YR\Desktop\Test.png", ImreadModes.Grayscale);

            // 截取屏幕
            Mat screen = CaptureScreen();

            // 使用模板匹配查找模板图片在屏幕中的位置
            Rectangle[] matches = FindTemplate(screen, template);

            if (matches.Length > 0)
            {
                Console.WriteLine("模板图片找到，位置：");
                foreach (var match in matches)
                {
                    Console.WriteLine($"X: {match.X}, Y: {match.Y}");
                }
            }
            else
            {
                Console.WriteLine("模板图片未找到");
            }

            // 释放图像资源
            template.Dispose();
            screen.Dispose();
        }

        static Mat CaptureScreen(MatchOptions matchOptions)
        {

            if (matchOptions.MatchMode == MatchMode.Absolutely)
            {
                //ImageHelper.GetFullScreen(FullScreenImage);
                //Src = CvInvoke.Imread(FullScreenImage, matchOptions.ImreadModes);
                Src = CaptureScreen(ImreadModes.Grayscale);
            }
            else
            {
                ImageHelper.GetAndSaveSpecificScreenArea(PartialScreenImage, matchOptions.WindowArea);
                Src = CvInvoke.Imread(PartialScreenImage, matchOptions.ImreadModes);
            }

            string tempFileName = System.IO.Path.GetTempFileName();
            if (rectangle == default)
            {
                Rectangle screenSize = System.Windows.Forms.Screen.PrimaryScreen.Bounds;
                Bitmap bitmap = new Bitmap(screenSize.Width, screenSize.Height);
                using (Graphics g = Graphics.FromImage(bitmap))
                {
                    g.CopyFromScreen(screenSize.Left, screenSize.Top, 0, 0, screenSize.Size);
                }
                return BitmapToMat(bitmap);
            }
            else
            {
                Bitmap partialScreen = new Bitmap(rectangle.Width, rectangle.Height);
                using (Graphics g = Graphics.FromImage(partialScreen))
                {
                    g.CopyFromScreen(rectangle.X, rectangle.Y, 0, 0, rectangle.Size);
                }
                return BitmapToMat(partialScreen);
            }
        }

        static Mat BitmapToMat(Bitmap bitmap)
        {
            Mat mat = new Mat();
            BitmapData data = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height), ImageLockMode.ReadOnly, bitmap.PixelFormat);
            CvInvoke.cvSetData(mat, data.Scan0, data.Stride);
            bitmap.UnlockBits(data);
            return mat;
        }

        static Rectangle[] FindTemplate(Mat source, Mat template)
        {
            using (Mat result = new Mat())
            {
                CvInvoke.MatchTemplate(source, template, result, TemplateMatchingType.CcoeffNormed);
                double minVal = 0.0;
                double maxVal = 0.0;
                Point minLoc = new Point();
                Point maxLoc = new Point();
                CvInvoke.MinMaxLoc(result, ref minVal, ref maxVal, ref minLoc, ref maxLoc);

                if (maxVal >= 0.9) // 设置相似度阈值
                {
                    Rectangle match = new Rectangle(maxLoc, template.Size);
                    return new[] { match };
                }
                else
                {
                    return new Rectangle[0];
                }
            }
        }



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
            CvInvoke.MatchTemplate(Src, Template, MatchResult, TemplateMatchingType.CcorrNormed);//使用相关系数法匹配
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
                //ImageHelper.GetFullScreen(FullScreenImage);
                //Src = CvInvoke.Imread(FullScreenImage, matchOptions.ImreadModes);
                Src = CaptureScreen(ImreadModes.Grayscale);
            }
            else
            {
                ImageHelper.GetAndSaveSpecificScreenArea(PartialScreenImage, matchOptions.WindowArea);
                Src = CvInvoke.Imread(PartialScreenImage, matchOptions.ImreadModes);
            }

            //加载模板图片
            Mat Template = CvInvoke.Imread(img, matchOptions.ImreadModes);

            Mat MatchResult = new Mat();//匹配结果
            CvInvoke.MatchTemplate(Src, Template, MatchResult, TemplateMatchingType.CcorrNormed);//使用相关系数法匹配

    
            Point max_loc = new Point();
            Point min_loc = new Point();
            double max = 0, min = 0;
            CvInvoke.MinMaxLoc(MatchResult, ref min, ref max, ref min_loc, ref max_loc);//获得极值信息

            //int[] MinIdx = new int[10];
            //int[] MaxIdx = new int[10];
            //CvInvoke.MinMaxIdx(MatchResult, out double min1, out double max1, MinIdx, MaxIdx);//获得极值信息

            //MatchResult.MinMax(out double[] minValues, out double[] maxValues, out Point[] minLocations, out Point[] maxLocations);
            Similarity = max;
            if (max > matchOptions.Threshold)
            {
                return new Rectangle(max_loc, Template.Size);
            }
            else
            {
                return Rectangle.Empty;
            }
        }


    }



    public class MatchOptions
    {
        public MatchOptions()
        {

        }

        public MatchOptions(int maxtimes, int interval)
        {
            MaxTimes = maxtimes;
            DelayInterval = interval;
        }

        /// <summary>
        /// 设置或获取 最大查找匹配次数，默认0 次将一直查找直到匹配成功
        /// </summary>
        public int MaxTimes { get; set; }

        /// <summary>
        /// 设置或获取 每次查找匹配的间隔时间，单位ms
        /// </summary>
        public int DelayInterval { get; set; }

        /// <summary>
        /// 设置或获取 匹配度阈值，越接近1则，相似匹配度越高
        /// </summary>
        public double Threshold { get; set; } = 0.98;

        /// <summary>
        /// 设置或获取 匹配模式, 默认为全屏查找
        /// </summary>
        public MatchMode MatchMode { get; set; }

        private Rectangle _WindowArea;
        /// <summary>
        /// 设置或获取 匹配区域，当MatchMode为Relatively，使用WindowArea在指定区域进行匹配
        /// </summary>
        public Rectangle WindowArea
        {
            get
            {
                if (MatchMode == MatchMode.Absolutely)
                    return Rectangle.Empty;
                else
                    return _WindowArea;
            }
            set
            {
                _WindowArea = value;
            }
        }

        /// <summary>
        /// 对图片加载处理模式 ，默认为Grayscale；
        /// </summary>
        public ImreadModes ImreadModes { get; set; } = ImreadModes.Grayscale;

        public ImreadModesConvert ImreadModesConvert
        {
            get
            {
                return (ImreadModesConvert)ImreadModes;
            }
            set
            {
                ImreadModes = (ImreadModes)value;
            }
        }
    }

    /// <summary>
    /// 匹配模式
    /// </summary>
    public enum MatchMode
    {
        /// <summary>
        /// 完全匹配，相对整个屏幕
        /// </summary>
        Absolutely,

        /// <summary>
        /// 相对模式，相对窗口的位置
        /// </summary>
        Relatively
    }

    /// <summary>
    /// cvLoadImage type
    /// </summary>
    [Flags]
    public enum ImreadModesConvert
    {
        /// <summary>
        ///  If set, return the loaded image as is (with alpha channel, otherwise it gets cropped).
        /// </summary>
        Unchanged = -1,

        /// <summary>
        /// If set, always convert image to the single channel grayscale image.
        /// </summary>
        Grayscale = 0,

        /// <summary>
        /// If set, always convert image to the 3 channel BGR color image.
        /// </summary>
        Color = 1,

        /// <summary>
        /// If set, return 16-bit/32-bit image when the input has the corresponding depth,otherwise convert it to 8-bit.
        /// </summary>
        AnyDepth = 2,

        /// <summary>
        ///  If set, the image is read in any possible color format.
        /// </summary>
        AnyColor = 4,

        /// <summary>
        /// If set, use the gdal driver for loading the image.
        /// </summary>
        LoadGdal = 8,

        /// <summary>
        ///  If set, always convert image to the single channel grayscale image and the image size reduced 1/2.
        /// </summary>
        ReducedGrayscale2 = 16,

        /// <summary>
        ///  If set, always convert image to the 3 channel BGR color image and the image size reduced 1/2.
        /// </summary>
        ReducedColor2 = 17,

        /// <summary>
        ///  If set, always convert image to the single channel grayscale image and the image size reduced 1/4.
        /// </summary>
        ReducedGrayscale4 = 32,

        /// <summary>
        /// If set, always convert image to the 3 channel BGR color image and the image size reduced 1/4.
        /// </summary>
        ReducedColor4 = 33,

        /// <summary>
        /// If set, always convert image to the single channel grayscale image and the image size reduced 1/8.
        /// </summary>
        ReducedGrayscale8 = 64,

        /// <summary>
        /// If set, always convert image to the 3 channel BGR color image and the image size reduced 1/8.
        /// </summary>
        ReducedColor8 = 65,

        /// <summary>
        /// If set, do not rotate the image according to EXIF's orientation flag.
        /// </summary>
        IgnoreOrientation = 128
    }
}
