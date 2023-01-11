using OpenCvSharp;
using OpenCvSharp.Extensions;
using OpenCvSharp.ML;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Text;

namespace AutomationServices.EmguCv.Helper
{
    public class OpenCvSharpHelper
    {

        //public static Bitmap MatchPicBySift(Bitmap imgSrc, Bitmap imgSub)
        //{
        //    using (Mat matSrc = imgSrc.ToMat())
        //    using (Mat matTo = imgSub.ToMat())
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


        public static Bitmap MatchPicBySurf(Bitmap imgSrc, Bitmap imgSub, double threshold = 400)
        {
            using (Mat matSrc = imgSrc.ToMat())
            using (Mat matTo = imgSub.ToMat())
            using (Mat matSrcRet = new Mat())
            using (Mat matToRet = new Mat())
            {
                KeyPoint[] keyPointsSrc, keyPointsTo;
                using (var surf = OpenCvSharp.XFeatures2D.SURF.Create(threshold, 4, 3, true, true))
                {
                    surf.DetectAndCompute(matSrc, null, out keyPointsSrc, matSrcRet);
                    surf.DetectAndCompute(matTo, null, out keyPointsTo, matToRet);
                }
                using (var flnMatcher = new OpenCvSharp.FlannBasedMatcher())
                {
                    var matches = flnMatcher.Match(matSrcRet, matToRet);
                    //求最小最大距离
                    double minDistance = 1000;//反向逼近
                    double maxDistance = 0;
                    for (int i = 0; i < matSrcRet.Rows; i++)
                    {
                        double distance = matches[i].Distance;
                        if (distance > maxDistance)
                        {
                            maxDistance = distance;
                        }
                        if (distance < minDistance)
                        {
                            minDistance = distance;
                        }
                    }
                    Console.WriteLine($"max distance : {maxDistance}");
                    Console.WriteLine($"min distance : {minDistance}");
                    var pointsSrc = new List<Point2f>();
                    var pointsDst = new List<Point2f>();
                    //筛选较好的匹配点
                    var goodMatches = new List<DMatch>();
                    for (int i = 0; i < matSrcRet.Rows; i++)
                    {
                        double distance = matches[i].Distance;
                        if (distance < Math.Max(minDistance * 2, 0.02))
                        {
                            pointsSrc.Add(keyPointsSrc[matches[i].QueryIdx].Pt);
                            pointsDst.Add(keyPointsTo[matches[i].TrainIdx].Pt);
                            //距离小于范围的压入新的DMatch
                            goodMatches.Add(matches[i]);
                        }
                    }
                    var outMat = new Mat();
                    // 算法RANSAC对匹配的结果做过滤
                    var pSrc = pointsSrc.ConvertAll(Point2fToPoint2dConveter);
                    var pDst = pointsDst.ConvertAll(Point2fToPoint2dConveter);
                    var outMask = new Mat();
                    // 如果原始的匹配结果为空, 则跳过过滤步骤
                    if (pSrc.Count > 0 && pDst.Count > 0)
                        Cv2.FindHomography(pSrc, pDst, HomographyMethods.Ransac, mask: outMask);
                    // 如果通过RANSAC处理后的匹配点大于10个,才应用过滤. 否则使用原始的匹配点结果(匹配点过少的时候通过RANSAC处理后,可能会得到0个匹配点的结果).
                    if (outMask.Rows > 10)
                    {
                        byte[] maskBytes = new byte[outMask.Rows * outMask.Cols];
                        //outMask.GetArray(0, 0, maskBytes);
                        outMask.GetArray(out maskBytes);
                        Cv2.DrawMatches(matSrc, keyPointsSrc, matTo, keyPointsTo, goodMatches, outMat, matchesMask: maskBytes, flags: DrawMatchesFlags.NotDrawSinglePoints);
                    }
                    else
                        Cv2.DrawMatches(matSrc, keyPointsSrc, matTo, keyPointsTo, goodMatches, outMat, flags: DrawMatchesFlags.NotDrawSinglePoints);
                    return OpenCvSharp.Extensions.BitmapConverter.ToBitmap(outMat);
                }
            }
        }

        static Point2d Point2fToPoint2dConveter(Point2f p2f)
        {
            return new Point2d(p2f.X, p2f.Y);
        }


        public static System.Drawing.Point FindPicFromImage(Bitmap imgSrc, Bitmap imgSub, double threshold = 0.9)
        {
            OpenCvSharp.Mat srcMat = null;
            OpenCvSharp.Mat dstMat = null;
            OpenCvSharp.OutputArray outArray = null;
            try
            {
                srcMat = imgSrc.ToMat();
                dstMat = imgSub.ToMat();
                outArray = OpenCvSharp.OutputArray.Create(srcMat);
                //OpenCvSharp.Cv2.MatchTemplate(srcMat, dstMat, outArray, Common.templateMatchModes);
                OpenCvSharp.Cv2.MatchTemplate(srcMat, dstMat, outArray, TemplateMatchModes.CCorrNormed);
                double minValue, maxValue;
                OpenCvSharp.Point location, point;
                OpenCvSharp.Cv2.MinMaxLoc(OpenCvSharp.InputArray.Create(outArray.GetMat()), out minValue, out maxValue, out location, out point);
                Console.WriteLine(maxValue);
                if (maxValue >= threshold)
                    return new System.Drawing.Point(point.X, point.Y);
                return System.Drawing.Point.Empty;
            }
            catch (Exception ex)
            {
                return System.Drawing.Point.Empty;
            }
            finally
            {
                if (srcMat != null)
                    srcMat.Dispose();
                if (dstMat != null)
                    dstMat.Dispose();
                if (outArray != null)
                    outArray.Dispose();
            }
        }




        #region HOG

        //准备数据，即分割digits.png得到一个个独立的样本数据。
        public static void SplitDigits(string path)
        {
            Mat gray = new Mat(path, ImreadModes.Grayscale);

            int imgName = 0;
            int imgIndex = 0;

            int step = 20;
            int rowsCount = gray.Rows / step;   //原图为1000*2000
            int colsCount = gray.Cols / step;   //裁剪为5000个20*20的小图块
            for (int i = 0; i < rowsCount; i++)
            {
                if (i % 5 == 0 && i != 0)
                {
                    imgName++;
                    imgIndex = 0;
                }

                int offsetRow = i * step;  //行上的偏移量
                for (int j = 0; j < colsCount; j++)
                {
                    int offsetCol = j * step; //列上的偏移量
                    Mat temp = gray.SubMat(offsetRow, offsetRow + step, offsetCol, offsetCol + step);
                    if (!System.IO.Directory.Exists($"svm/digits/{imgName}"))// OpenCV不会自动创建目录，此处需要代码额外处理下
                    {
                        System.IO.Directory.CreateDirectory($"digits/{imgName}");
                    }
                    temp.ImWrite($"digits/{imgName}/{imgIndex}.png");
                    imgIndex++;
                }
            }

            Console.WriteLine("split complete");
        }

        //准备数据
        static void PrepareData(out Mat tTrainData, out Mat tTrainLabel, out Mat tTestData, out Mat tTestLabel)
        {
            tTrainData = new Mat();
            tTrainLabel = new Mat();
            tTestData = new Mat();
            tTestLabel = new Mat();
            for (int i = 0; i < 10; i++)
            {
                for (int j = 0; j < 500; j++)
                {
                    Mat temp = new Mat($"digits/{i}/{j}.png", ImreadModes.Grayscale);
                    temp = temp.Reshape(1, 1);
                    if (j < 400)
                    {
                        tTrainData.PushBack(temp);
                        tTrainLabel.PushBack(i);
                    }
                    else
                    {
                        tTestData.PushBack(temp);
                        tTestLabel.PushBack(i);
                    }
                }
            }
            tTrainData.ConvertTo(tTrainData, MatType.CV_32F);
            tTestData.ConvertTo(tTestData, MatType.CV_32F);
            tTestLabel.ConvertTo(tTestLabel, MatType.CV_32F);
        }


        //预测
        static void Predict(KNearest knn)
        {
            //Mat tTestData = new Mat();

            //Mat temp = new Mat($"digits/7/30.png", ImreadModes.Grayscale);
            //temp = temp.Reshape(1, 1);
            //tTestData.PushBack(temp);

            //temp = new Mat($"digits/0/355.png", ImreadModes.Grayscale);
            //temp = temp.Reshape(1, 1);
            //tTestData.PushBack(temp);
            //tTestData.ConvertTo(tTestData, MatType.CV_32F);

            //Mat testPredict = new Mat();
            //knn.Predict(tTestData, testPredict);
            //testPredict.ConvertTo(testPredict, MatType.CV_8U);
            //byte num = testPredict.At<byte>(0, 0);
            //byte num1 = testPredict.At<byte>(1, 0);
            //Console.WriteLine($"图片的数值{num} {num1}");



            Mat tTestData = new Mat();
            Mat temp = new Mat(@"C:\Users\YR\Desktop\2.png", ImreadModes.Grayscale);
            temp = temp.Reshape(1, 1);
            tTestData.PushBack(temp);

            temp = new Mat($"digits/6/355.png", ImreadModes.Grayscale);
            temp = temp.Reshape(1, 1);
            tTestData.PushBack(temp);
            tTestData.ConvertTo(tTestData, MatType.CV_32F);

            Mat testPredict = new Mat();
            knn.Predict(tTestData, testPredict);
            testPredict.ConvertTo(testPredict, MatType.CV_8U);
            byte num = testPredict.At<byte>(0, 0);
            byte num1 = testPredict.At<byte>(1, 0);
            Console.WriteLine($"图片的数值{num} {num1}");
        }


        //训练，存储训练出来的模型
        static void TrainAndSaveModel()
        {
            //KNearest knn = KNearest.Create();
            //knn.Train(tTrainData, SampleTypes.RowSample, tTrainLabel);

            //knn.Save("digits/knn.xml");
            /*knn.Read(new FileStorage("digits/knn.xml", FileStorage.Mode.Read).GetFirstTopLevelNode()); 下一次可以直接读取存储的模型，而不用再次训练*/
        }


        //测试验证模型准确率
        public static void TestVerify(KNearest knn, Mat tTestData, Mat tTestLabel)
        {
            Mat testPredict = new Mat();
            knn.Predict(tTestData, testPredict);
            Mat errorImg = tTestLabel.NotEquals(testPredict);
            float errorPercent = 100f * errorImg.CountNonZero() / testPredict.Rows;
            Console.WriteLine($"错误率：{errorPercent}");
        }


        public static void Train()
        {
            PrepareData(out Mat tTrainData, out Mat tTrainLabel, out Mat tTestData, out Mat tTestLabel);
            KNearest knn = KNearest.Create();

            knn.Train(tTrainData, SampleTypes.RowSample, tTrainLabel);
            knn.Save("digits/knn.xml");

            TestVerify(knn, tTestData, tTestLabel);
            //knn.Read(new FileStorage("digits/knn.xml", FileStorage.Modes.Read).GetFirstTopLevelNode()); //下一次可以直接读取存储的模型，而不用再次训练*/
        }


        public static void LoadKnnAndPredict()
        {
            KNearest knn = KNearest.Create();
            knn.Read(new FileStorage("digits/knn.xml", FileStorage.Modes.Read).GetFirstTopLevelNode()); //下一次可以直接读取存储的模型，而不用再次训练*/

            Predict(knn);
        }

        #endregion
    }

}
