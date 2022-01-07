using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
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
//using OpenCvSharp;
//using OpenCvSharp.CPlusPlus;
//using OpenCvSharp.CPlusPlus;

namespace OpenCV
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : System.Windows.Window
    {
        public MainWindow()
        {
            InitializeComponent();
            //Test();

            var findpic = new FindPic();
            //查找微云图片
            var rec = findpic.FindPicture(findImage, sourceImage,10);
            Console.WriteLine(rec[0]);
        }

        //public static TemplateMatchModes templateMatchModes = TemplateMatchModes.CCoeffNormed;

        public string sourceImage = @"C:\Users\YR\Desktop\1.png";
        public string findImage = @"C:\Users\YR\Desktop\2.png";

        ////public string 

        //void Test()
        //{
        //    var bt1 = new Bitmap(sourceImage);
        //    var bt2 = new Bitmap(findImage);

        //    //var Sift = MatchPicBySift(sourceImage, findImage);
        //    var Sift = MatchPicBySift(bt1, bt2);
        //    //var Surf = MatchPicBySurf(bt1, bt2);

        //    //var text = FindPicFromImage(Sift, Surf);
        //}

        //#region 2
        //public static Bitmap MatchPicBySift(string imgSrc, string imgSub)
        //{
        //    using (Mat matSrc = ToMat(imgSrc))
        //    using (Mat matTo = ToMat(imgSub))
        //    using (Mat matSrcRet = new Mat())
        //    using (Mat matToRet = new Mat())
        //    {
        //        KeyPoint[] keyPointsSrc, keyPointsTo;
        //        using (var sift = OpenCvSharp.XFeatures2D.SIFT.Create())
        //        {
        //            sift.DetectAndCompute(matSrc, null, out keyPointsSrc, matSrcRet);
        //            sift.DetectAndCompute(matTo, null, out keyPointsTo, matToRet);
        //        }
        //        using (var bfMatcher = new OpenCvSharp.BFMatcher())
        //        {
        //            var matches = bfMatcher.KnnMatch(matSrcRet, matToRet, k: 2);

        //            var pointsSrc = new List<Point2f>();
        //            var pointsDst = new List<Point2f>();
        //            var goodMatches = new List<DMatch>();
        //            foreach (DMatch[] items in matches.Where(x => x.Length > 1))
        //            {
        //                if (items[0].Distance < 0.5 * items[1].Distance)
        //                {
        //                    pointsSrc.Add(keyPointsSrc[items[0].QueryIdx].Pt);
        //                    pointsDst.Add(keyPointsTo[items[0].TrainIdx].Pt);
        //                    goodMatches.Add(items[0]);
        //                    Console.WriteLine($"{keyPointsSrc[items[0].QueryIdx].Pt.X}, {keyPointsSrc[items[0].QueryIdx].Pt.Y}");
        //                }
        //            }

        //            var outMat = new Mat();

        //            // 算法RANSAC对匹配的结果做过滤
        //            var pSrc = pointsSrc.ConvertAll(Point2fToPoint2d);
        //            var pDst = pointsDst.ConvertAll(Point2fToPoint2d);
        //            var outMask = new Mat();
        //            // 如果原始的匹配结果为空, 则跳过过滤步骤
        //            if (pSrc.Count > 0 && pDst.Count > 0)
        //                Cv2.FindHomography(pSrc, pDst, HomographyMethods.Ransac, mask: outMask);
        //            // 如果通过RANSAC处理后的匹配点大于10个,才应用过滤. 否则使用原始的匹配点结果(匹配点过少的时候通过RANSAC处理后,可能会得到0个匹配点的结果).
        //            if (outMask.Rows > 10)
        //            {
        //                byte[] maskBytes = new byte[outMask.Rows * outMask.Cols];
        //                outMask.GetArray(0, 0, maskBytes);
        //                Cv2.DrawMatches(matSrc, keyPointsSrc, matTo, keyPointsTo, goodMatches, outMat, matchesMask: maskBytes, flags: DrawMatchesFlags.NotDrawSinglePoints);
        //            }
        //            else
        //                Cv2.DrawMatches(matSrc, keyPointsSrc, matTo, keyPointsTo, goodMatches, outMat, flags: DrawMatchesFlags.NotDrawSinglePoints);
        //            return OpenCvSharp.Extensions.BitmapConverter.ToBitmap(outMat);
        //        }
        //    }
        //}

        //public static Bitmap MatchPicBySurf(string imgSrc, string imgSub, double threshold = 400)
        //{
        //    using (Mat matSrc = ToMat(imgSrc))
        //    using (Mat matTo = ToMat(imgSub))
        //    using (Mat matSrcRet = new Mat())
        //    using (Mat matToRet = new Mat())
        //    {
        //        KeyPoint[] keyPointsSrc, keyPointsTo;
        //        using (var surf = OpenCvSharp.XFeatures2D.SURF.Create(threshold, 4, 3, true, true))
        //        {
        //            surf.DetectAndCompute(matSrc, null, out keyPointsSrc, matSrcRet);
        //            surf.DetectAndCompute(matTo, null, out keyPointsTo, matToRet);
        //        }

        //        using (var flnMatcher = new OpenCvSharp.FlannBasedMatcher())
        //        {
        //            var matches = flnMatcher.Match(matSrcRet, matToRet);
        //            //求最小最大距离
        //            double minDistance = 1000;//反向逼近
        //            double maxDistance = 0;
        //            for (int i = 0; i < matSrcRet.Rows; i++)
        //            {
        //                double distance = matches[i].Distance;
        //                if (distance > maxDistance)
        //                {
        //                    maxDistance = distance;
        //                }
        //                if (distance < minDistance)
        //                {
        //                    minDistance = distance;
        //                }
        //            }
        //            Console.WriteLine($"max distance : {maxDistance}");
        //            Console.WriteLine($"min distance : {minDistance}");

        //            var pointsSrc = new List<Point2f>();
        //            var pointsDst = new List<Point2f>();
        //            //筛选较好的匹配点
        //            var goodMatches = new List<DMatch>();
        //            for (int i = 0; i < matSrcRet.Rows; i++)
        //            {
        //                double distance = matches[i].Distance;
        //                if (distance < Math.Max(minDistance * 2, 0.02))
        //                {
        //                    pointsSrc.Add(keyPointsSrc[matches[i].QueryIdx].Pt);
        //                    pointsDst.Add(keyPointsTo[matches[i].TrainIdx].Pt);
        //                    //距离小于范围的压入新的DMatch
        //                    goodMatches.Add(matches[i]);
        //                }
        //            }

        //            var outMat = new Mat();

        //            // 算法RANSAC对匹配的结果做过滤
        //            var pSrc = pointsSrc.ConvertAll(Point2fToPoint2d);
        //            var pDst = pointsDst.ConvertAll(Point2fToPoint2d);
        //            var outMask = new Mat();
        //            // 如果原始的匹配结果为空, 则跳过过滤步骤
        //            if (pSrc.Count > 0 && pDst.Count > 0)
        //                Cv2.FindHomography(pSrc, pDst, HomographyMethods.Ransac, mask: outMask);
        //            // 如果通过RANSAC处理后的匹配点大于10个,才应用过滤. 否则使用原始的匹配点结果(匹配点过少的时候通过RANSAC处理后,可能会得到0个匹配点的结果).
        //            if (outMask.Rows > 10)
        //            {
        //                byte[] maskBytes = new byte[outMask.Rows * outMask.Cols];
        //                outMask.GetArray(0, 0, maskBytes);
        //                Cv2.DrawMatches(matSrc, keyPointsSrc, matTo, keyPointsTo, goodMatches, outMat, matchesMask: maskBytes, flags: DrawMatchesFlags.NotDrawSinglePoints);
        //            }
        //            else
        //                Cv2.DrawMatches(matSrc, keyPointsSrc, matTo, keyPointsTo, goodMatches, outMat, flags: DrawMatchesFlags.NotDrawSinglePoints);
        //            return OpenCvSharp.Extensions.BitmapConverter.ToBitmap(outMat);
        //        }
        //    }
        //}

        //public static System.Drawing.Point FindPicFromImage(string imgSrc, string imgSub, double threshold = 0.9)
        //{
        //    OpenCvSharp.Mat srcMat = null;
        //    OpenCvSharp.Mat dstMat = null;
        //    OpenCvSharp.OutputArray outArray = null;
        //    try
        //    {
        //        srcMat = ToMat(imgSrc);
        //        dstMat = ToMat(imgSub);
        //        outArray = OpenCvSharp.OutputArray.Create(srcMat);

        //        //OpenCvSharp.Cv2.MatchTemplate(srcMat, dstMat, outArray, Common.templateMatchModes);
        //        OpenCvSharp.Cv2.MatchTemplate(srcMat, dstMat, outArray, templateMatchModes);
        //        double minValue, maxValue;
        //        OpenCvSharp.Point location, point;
        //        OpenCvSharp.Cv2.MinMaxLoc(OpenCvSharp.InputArray.Create(outArray.GetMat()), out minValue, out maxValue, out location, out point);
        //        Console.WriteLine(maxValue);
        //        if (maxValue >= threshold)
        //            return new System.Drawing.Point(point.X, point.Y);
        //        return System.Drawing.Point.Empty;
        //    }
        //    catch (Exception ex)
        //    {
        //        return System.Drawing.Point.Empty;
        //    }
        //    finally
        //    {
        //        if (srcMat != null)
        //            srcMat.Dispose();
        //        if (dstMat != null)
        //            dstMat.Dispose();
        //        if (outArray != null)
        //            outArray.Dispose();
        //    }
        //}
        //#endregion

        //#region 1
        //public static Bitmap MatchPicBySift(Bitmap imgSrc, Bitmap imgSub)
        //{
        //    using (Mat matSrc = ToMat(imgSrc))
        //    using (Mat matTo = ToMat(imgSub))
        //    using (Mat matSrcRet = new Mat())
        //    using (Mat matToRet = new Mat())
        //    {
        //        KeyPoint[] keyPointsSrc, keyPointsTo;
        //        using (var sift = OpenCvSharp.XFeatures2D.SIFT.Create())
        //        {
        //            sift.DetectAndCompute(matSrc, null, out keyPointsSrc, matSrcRet);
        //            sift.DetectAndCompute(matTo, null, out keyPointsTo, matToRet);
        //        }
        //        using (var bfMatcher = new OpenCvSharp.BFMatcher())
        //        {
        //            var matches = bfMatcher.KnnMatch(matSrcRet, matToRet, k: 2);

        //            var pointsSrc = new List<Point2f>();
        //            var pointsDst = new List<Point2f>();
        //            var goodMatches = new List<DMatch>();
        //            foreach (DMatch[] items in matches.Where(x => x.Length > 1))
        //            {
        //                if (items[0].Distance < 0.5 * items[1].Distance)
        //                {
        //                    pointsSrc.Add(keyPointsSrc[items[0].QueryIdx].Pt);
        //                    pointsDst.Add(keyPointsTo[items[0].TrainIdx].Pt);
        //                    goodMatches.Add(items[0]);
        //                    Console.WriteLine($"{keyPointsSrc[items[0].QueryIdx].Pt.X}, {keyPointsSrc[items[0].QueryIdx].Pt.Y}");
        //                }
        //            }

        //            var outMat = new Mat();

        //            // 算法RANSAC对匹配的结果做过滤
        //            var pSrc = pointsSrc.ConvertAll(Point2fToPoint2d);
        //            var pDst = pointsDst.ConvertAll(Point2fToPoint2d);
        //            var outMask = new Mat();
        //            // 如果原始的匹配结果为空, 则跳过过滤步骤
        //            if (pSrc.Count > 0 && pDst.Count > 0)
        //                Cv2.FindHomography(pSrc, pDst, HomographyMethods.Ransac, mask: outMask);
        //            // 如果通过RANSAC处理后的匹配点大于10个,才应用过滤. 否则使用原始的匹配点结果(匹配点过少的时候通过RANSAC处理后,可能会得到0个匹配点的结果).
        //            if (outMask.Rows > 10)
        //            {
        //                byte[] maskBytes = new byte[outMask.Rows * outMask.Cols];
        //                outMask.GetArray(0, 0, maskBytes);
        //                Cv2.DrawMatches(matSrc, keyPointsSrc, matTo, keyPointsTo, goodMatches, outMat, matchesMask: maskBytes, flags: DrawMatchesFlags.NotDrawSinglePoints);
        //            }
        //            else
        //                Cv2.DrawMatches(matSrc, keyPointsSrc, matTo, keyPointsTo, goodMatches, outMat, flags: DrawMatchesFlags.NotDrawSinglePoints);
        //            return OpenCvSharp.Extensions.BitmapConverter.ToBitmap(outMat);
        //        }
        //    }
        //}

        //public static Bitmap MatchPicBySurf(Bitmap imgSrc, Bitmap imgSub, double threshold = 400)
        //{
        //    using (Mat matSrc = ToMat(imgSrc))
        //    using (Mat matTo = ToMat(imgSub))
        //    using (Mat matSrcRet = new Mat())
        //    using (Mat matToRet = new Mat())
        //    {
        //        KeyPoint[] keyPointsSrc, keyPointsTo;
        //        using (var surf = OpenCvSharp.XFeatures2D.SURF.Create(threshold, 4, 3, true, true))
        //        {
        //            surf.DetectAndCompute(matSrc, null, out keyPointsSrc, matSrcRet);
        //            surf.DetectAndCompute(matTo, null, out keyPointsTo, matToRet);
        //        }

        //        using (var flnMatcher = new OpenCvSharp.FlannBasedMatcher())
        //        {
        //            var matches = flnMatcher.Match(matSrcRet, matToRet);
        //            //求最小最大距离
        //            double minDistance = 1000;//反向逼近
        //            double maxDistance = 0;
        //            for (int i = 0; i < matSrcRet.Rows; i++)
        //            {
        //                double distance = matches[i].Distance;
        //                if (distance > maxDistance)
        //                {
        //                    maxDistance = distance;
        //                }
        //                if (distance < minDistance)
        //                {
        //                    minDistance = distance;
        //                }
        //            }
        //            Console.WriteLine($"max distance : {maxDistance}");
        //            Console.WriteLine($"min distance : {minDistance}");

        //            var pointsSrc = new List<Point2f>();
        //            var pointsDst = new List<Point2f>();
        //            //筛选较好的匹配点
        //            var goodMatches = new List<DMatch>();
        //            for (int i = 0; i < matSrcRet.Rows; i++)
        //            {
        //                double distance = matches[i].Distance;
        //                if (distance < Math.Max(minDistance * 2, 0.02))
        //                {
        //                    pointsSrc.Add(keyPointsSrc[matches[i].QueryIdx].Pt);
        //                    pointsDst.Add(keyPointsTo[matches[i].TrainIdx].Pt);
        //                    //距离小于范围的压入新的DMatch
        //                    goodMatches.Add(matches[i]);
        //                }
        //            }

        //            var outMat = new Mat();

        //            // 算法RANSAC对匹配的结果做过滤
        //            var pSrc = pointsSrc.ConvertAll(Point2fToPoint2d);
        //            var pDst = pointsDst.ConvertAll(Point2fToPoint2d);
        //            var outMask = new Mat();
        //            // 如果原始的匹配结果为空, 则跳过过滤步骤
        //            if (pSrc.Count > 0 && pDst.Count > 0)
        //                Cv2.FindHomography(pSrc, pDst, HomographyMethods.Ransac, mask: outMask);
        //            // 如果通过RANSAC处理后的匹配点大于10个,才应用过滤. 否则使用原始的匹配点结果(匹配点过少的时候通过RANSAC处理后,可能会得到0个匹配点的结果).
        //            if (outMask.Rows > 10)
        //            {
        //                byte[] maskBytes = new byte[outMask.Rows * outMask.Cols];
        //                outMask.GetArray(0, 0, maskBytes);
        //                Cv2.DrawMatches(matSrc, keyPointsSrc, matTo, keyPointsTo, goodMatches, outMat, matchesMask: maskBytes, flags: DrawMatchesFlags.NotDrawSinglePoints);
        //            }
        //            else
        //                Cv2.DrawMatches(matSrc, keyPointsSrc, matTo, keyPointsTo, goodMatches, outMat, flags: DrawMatchesFlags.NotDrawSinglePoints);
        //            return OpenCvSharp.Extensions.BitmapConverter.ToBitmap(outMat);
        //        }
        //    }
        //}

        //public static System.Drawing.Point FindPicFromImage(Bitmap imgSrc, Bitmap imgSub, double threshold = 0.9)
        //{
        //    OpenCvSharp.Mat srcMat = null;
        //    OpenCvSharp.Mat dstMat = null;
        //    OpenCvSharp.OutputArray outArray = null;
        //    try
        //    {
        //        srcMat = ToMat(imgSrc);
        //        dstMat = ToMat(imgSub);
        //        outArray = OpenCvSharp.OutputArray.Create(srcMat);

        //        //OpenCvSharp.Cv2.MatchTemplate(srcMat, dstMat, outArray, Common.templateMatchModes);
        //        OpenCvSharp.Cv2.MatchTemplate(srcMat, dstMat, outArray, templateMatchModes);
        //        double minValue, maxValue;
        //        OpenCvSharp.Point location, point;
        //        OpenCvSharp.Cv2.MinMaxLoc(OpenCvSharp.InputArray.Create(outArray.GetMat()), out minValue, out maxValue, out location, out point);
        //        Console.WriteLine(maxValue);
        //        if (maxValue >= threshold)
        //            return new System.Drawing.Point(point.X, point.Y);
        //        return System.Drawing.Point.Empty;
        //    }
        //    catch (Exception ex)
        //    {
        //        return System.Drawing.Point.Empty;
        //    }
        //    finally
        //    {
        //        if (srcMat != null)
        //            srcMat.Dispose();
        //        if (dstMat != null)
        //            dstMat.Dispose();
        //        if (outArray != null)
        //            outArray.Dispose();
        //    }
        //}
        //#endregion


        //private static Point2d Point2fToPoint2d(Point2f input)
        //{
        //    Point2d p2 = new Point2d(input.X, input.Y);
        //    return p2;
        //}


        //public static Mat ToMat(Bitmap image)
        //{
        //     using (Mat mat = OpenCvSharp.Extensions.BitmapConverter.ToMat(image))
        //    //Bitmap bitmap = OpenCvSharp.Extensions.BitmapConverter.ToBitmap(mat); // mat 转 bitmap
        //    return mat;
        //}

        //[DllImport("OpenCvSharpExtern.dll")]
        //public static extern string TestFunc1(string param1);
        //string ret1 = TestFunc1("text");


        //public static Mat ToMat(string name)
        //{
        //    Mat mat = new Mat(name); //bitmap转 mat
        //    return mat;
        //}

        /////// <summary>
        /////// bitmap 位图转为mat类型 
        /////// </summary>
        /////// <param name="bitmap"></param>
        /////// <returns></returns>
        ////public static Mat ToMat(Bitmap bitmap)
        ////{
        ////    MemoryStream s2_ms = null;
        ////    Mat source = null;
        ////    try
        ////    {
        ////        using (s2_ms = new MemoryStream())
        ////        {
        ////            bitmap.Save(s2_ms, ImageFormat.Bmp);
        ////            source = Mat.FromStream(s2_ms, ImreadModes.AnyColor);
        ////        }
        ////    }
        ////    catch (Exception e)
        ////    {
        ////        //log.Error(e.ToString());
        ////    }
        ////    finally
        ////    {
        ////        if (s2_ms != null)
        ////        {
        ////            s2_ms.Close();
        ////            s2_ms = null;
        ////        }
        ////        GC.Collect();
        ////    }
        ////    return source;
        ////}


    }
}
