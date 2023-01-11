using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Features2D;
using Emgu.CV.Structure;
using Emgu.CV.Util;
using Emgu.CV.XFeatures2D;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace AutomationServices.EmguCv.Helper
{
    public class EmguCvHelper
    {

        private static string FullScreenImage { get; set; } = AppDomain.CurrentDomain.BaseDirectory + "FullScreenImage.png";
        private static string PartialScreenImage { get; set; } = AppDomain.CurrentDomain.BaseDirectory + "PartialScreenImage.png";


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
        /// 
        /// </summary>
        /// <param name="img1">大图</param>
        /// <param name="img2">小图</param>
        /// <returns></returns>
        public static Rectangle SliderVerifi(string img1, string img2)
        {
            Mat Src = CvInvoke.Imread(img1, ImreadModes.Grayscale);
            Mat Template = CvInvoke.Imread(img2, ImreadModes.Grayscale);

            Template.Save(@"C:\Users\YR\Desktop\Template.png");
            for (int row = 0; row < Template.Height; row++)
            {
                for (int col = 0; col < Template.Width; col++)
                {
                    if ((Template.GetData() as byte[,])[row, col] == 0)
                    {
                        MatExtension.SetValue(Template, row, col, (byte)96);
                    }
                    else
                    {

                    }
                }
            }
            Template.Save(@"C:\Users\YR\Desktop\Template2.png");
            Mat Template3 = new Mat();
            //自适应阈值
            CvInvoke.AdaptiveThreshold(
                Template,
                Template3,
                255,
                AdaptiveThresholdType.GaussianC,
                ThresholdType.Binary,
                3,
                0);
            Template3.Save(@"C:\Users\YR\Desktop\Template3.png");

            Mat Template4 = new Mat();
            CvInvoke.Threshold(
                Template,
                Template4,
                96,
                96,
                ThresholdType.Binary
                );
            Template4.Save(@"C:\Users\YR\Desktop\Template4.png");


            Src.Save(@"C:\Users\YR\Desktop\Src.png");
            //for (int row = 0; row < Src.Height; row++)
            //{
            //    for (int col = 0; col < Src.Width; col++)
            //    {
            //        if ((Src.GetData() as byte[,])[row, col] == 0)
            //        {
            //            MatExtension.SetValue(Src, row, col, (byte)96);
            //        }
            //    }
            //}
            //Src.Save(@"C:\Users\YR\Desktop\Src2.png");
            Mat Src3 = new Mat();
            //自适应阈值
            CvInvoke.AdaptiveThreshold(
                Src,
                Src3,
                255,
                AdaptiveThresholdType.GaussianC,
                ThresholdType.Binary,
                3,
                0);
            Src3.Save(@"C:\Users\YR\Desktop\Src3.png");
            Mat Src4 = new Mat();
            CvInvoke.Threshold(
                Src,
                Src4,
                127,
                255,
                ThresholdType.Binary
                );
            Src4.Save(@"C:\Users\YR\Desktop\Src4.png");


            Mat MatchResult = new Mat();//匹配结果
            CvInvoke.MatchTemplate(Src4, Template4, MatchResult, TemplateMatchingType.CcoeffNormed);//使用相关系数法匹配
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
                ImageHelper.GetFullScreen(FullScreenImage);
                Src = CvInvoke.Imread(FullScreenImage, matchOptions.ImreadModes);
            }
            else
            {
                ImageHelper.GetAndSaveSpecificScreenArea(PartialScreenImage, matchOptions.WindowArea);
                Src = CvInvoke.Imread(PartialScreenImage, matchOptions.ImreadModes);
            }

            //Test(img, out Similarity, matchOptions.Threshold);

            Mat Template = CvInvoke.Imread(img, matchOptions.ImreadModes);

            Mat MatchResult = new Mat();//匹配结果
            CvInvoke.MatchTemplate(Src, Template, MatchResult, TemplateMatchingType.CcorrNormed);//使用相关系数法匹配

            var a = MatchResult.GetData() ;
            //Array.Sort(a);
            //float[,] bb = ((float[,])(a));
            //var c = bb[0][1]; 
            //List<(,)> n
            //CvInvoke.Threshold(MatchResult, MatchResult, 0.8, 1.0, ThresholdType.ToZero);
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



        public static Rectangle MatchPictureSimilarity(string img1, string img2, out double Similarity, ImreadModes loadType = ImreadModes.Color)
        {
            try
            {
                Mat Src = CvInvoke.Imread(img1, loadType);
                Mat Template = CvInvoke.Imread(img2, loadType);

                Mat MatchResult = new Mat();//匹配结果
                CvInvoke.MatchTemplate(Src, Template, MatchResult, TemplateMatchingType.CcorrNormed);//使用相关系数法匹配
                Point max_loc = new Point();
                Point min_loc = new Point();
                double max = 0, min = 0;
                CvInvoke.MinMaxLoc(MatchResult, ref min, ref max, ref min_loc, ref max_loc);//获得极值信息
                Similarity = max;
                return new Rectangle(max_loc, Template.Size);
            }
            catch (Exception ex)
            {
                throw ex;
            }
          
        }


    }

    public static class DrawMatches
    {
        public static void FindMatch(Mat modelImage, Mat observedImage, out long matchTime, out VectorOfKeyPoint modelKeyPoints, out VectorOfKeyPoint observedKeyPoints, VectorOfVectorOfDMatch matches, out Mat mask, out Mat homography)
        {
            int k = 2;
            double uniquenessThreshold = 0.8;
            double hessianThresh = 300;

            Stopwatch watch;
            homography = null;

            modelKeyPoints = new VectorOfKeyPoint();
            observedKeyPoints = new VectorOfKeyPoint();

            {
                using (UMat uModelImage = modelImage.GetUMat(AccessType.Read))
                using (UMat uObservedImage = observedImage.GetUMat(AccessType.Read))
                {
                    //Emgu.CV.XFeatures2D.Freak surfCPU = new Freak(hessianThresh);
                    Emgu.CV.XFeatures2D.Freak surfCPU = new Freak();
                    //extract features from the object image
                    UMat modelDescriptors = new UMat();
                    surfCPU.DetectAndCompute(uModelImage, null, modelKeyPoints, modelDescriptors, false);

                    watch = Stopwatch.StartNew();

                    // extract features from the observed image
                    UMat observedDescriptors = new UMat();
                    surfCPU.DetectAndCompute(uObservedImage, null, observedKeyPoints, observedDescriptors, false);
                    BFMatcher matcher = new BFMatcher(DistanceType.L2);
                    matcher.Add(modelDescriptors);

                    matcher.KnnMatch(observedDescriptors, matches, k, null);
                    mask = new Mat(matches.Size, 1, DepthType.Cv8U, 1);
                    mask.SetTo(new MCvScalar(255));
                    Features2DToolbox.VoteForUniqueness(matches, uniquenessThreshold, mask);

                    int nonZeroCount = CvInvoke.CountNonZero(mask);
                    if (nonZeroCount >= 4)
                    {
                        nonZeroCount = Features2DToolbox.VoteForSizeAndOrientation(modelKeyPoints, observedKeyPoints,
                           matches, mask, 1.5, 20);
                        if (nonZeroCount >= 4)
                            homography = Features2DToolbox.GetHomographyMatrixFromMatchedFeatures(modelKeyPoints,
                               observedKeyPoints, matches, mask, 2);
                    }

                    watch.Stop();
                }
            }
            matchTime = watch.ElapsedMilliseconds;
        }

        /// <summary>
        /// 绘制模型图像和观测图像，匹配特征和单应投影。
        /// </summary>
        /// <param name="modelImage">The model image</param>
        /// <param name="observedImage">The observed image</param>
        /// <param name="matchTime">计算单应矩阵的输出总时间.</param>
        /// <returns>模型图像和观测图像、匹配特征和单应投影。</returns>
        public static Mat Draw(Mat modelImage, Mat observedImage, out long matchTime)
        {
            Mat homography;
            VectorOfKeyPoint modelKeyPoints;
            VectorOfKeyPoint observedKeyPoints;
            using (VectorOfVectorOfDMatch matches = new VectorOfVectorOfDMatch())
            {
                Mat mask;
                FindMatch(modelImage, observedImage, out matchTime, out modelKeyPoints, out observedKeyPoints, matches,
                   out mask, out homography);

                //Draw the matched keypoints
                Mat result = new Mat();
                Features2DToolbox.DrawMatches(modelImage, modelKeyPoints, observedImage, observedKeyPoints,
                   matches, result, new MCvScalar(255, 255, 255), new MCvScalar(255, 255, 255), mask);

                #region draw the projected region on the image

                if (homography != null)
                {
                    //draw a rectangle along the projected model
                    Rectangle rect = new Rectangle(Point.Empty, modelImage.Size);
                    PointF[] pts = new PointF[]
                    {
                  new PointF(rect.Left, rect.Bottom),
                  new PointF(rect.Right, rect.Bottom),
                  new PointF(rect.Right, rect.Top),
                  new PointF(rect.Left, rect.Top)
                    };
                    pts = CvInvoke.PerspectiveTransform(pts, homography);

                    Point[] points = Array.ConvertAll<PointF, Point>(pts, Point.Round);
                    using (VectorOfPoint vp = new VectorOfPoint(points))
                    {
                        CvInvoke.Polylines(result, vp, true, new MCvScalar(255, 0, 0, 255), 5);
                    }

                }

                #endregion

                return result;

            }
        }
    }


    /// <summary>
    /// 通过指针操作内存，修改or获取Mat元素值
    /// </summary>
    public static class MatExtension
    {
        /// <summary>
        /// 获取Mat的元素值
        /// </summary>
        /// <param name="mat">需操作的Mat</param>
        /// <param name="row">元素行</param>
        /// <param name="col">元素列</param>
        /// <returns></returns>
        public static dynamic GetValue(this Mat mat, int row, int col)
        {
            var value = CreateElement(mat.Depth);
            Marshal.Copy(mat.DataPointer + (row * mat.Cols + col) * mat.ElementSize, value, 0, 1);
            return value[0];
        }
        /// <summary>
        /// 修改Mat的元素值
        /// </summary>
        /// <param name="mat">需操作的Mat</param>
        /// <param name="row">元素行</param>
        /// <param name="col">元素列</param>
        /// <param name="value">修改值</param>
        public static void SetValue(this Mat mat, int row, int col, dynamic value)
        {
            var target = CreateElement(mat.Depth, value);
            Marshal.Copy(target, 0, mat.DataPointer + (row * mat.Cols + col) * mat.ElementSize, 1);
        }
        /// <summary>
        /// 根据Mat的类型，动态解析传入数据的类型
        /// </summary>
        /// <param name="depthType"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        private static dynamic CreateElement(DepthType depthType, dynamic value)
        {
            var element = CreateElement(depthType);
            element[0] = value;
            return element;
        }
        /// <summary>
        /// 获取Mat元素的类型
        /// </summary>
        /// <param name="depthType"></param>
        /// <returns></returns>
        private static dynamic CreateElement(DepthType depthType)
        {
            if (depthType == DepthType.Cv8S)
            {
                return new sbyte[1];
            }
            if (depthType == DepthType.Cv8U)
            {
                return new byte[1];
            }
            if (depthType == DepthType.Cv16S)
            {
                return new short[1];
            }
            if (depthType == DepthType.Cv16U)
            {
                return new ushort[1];
            }
            if (depthType == DepthType.Cv32S)
            {
                return new int[1];
            }
            if (depthType == DepthType.Cv32F)
            {
                return new float[1];
            }
            if (depthType == DepthType.Cv64F)
            {
                return new double[1];
            }
            return new float[1];
        }
    }
}
