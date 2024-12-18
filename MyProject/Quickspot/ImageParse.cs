﻿using AutomationServices.EmguCv.Helper;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Quickspot
{
    class ImageParse
    {
        public static List<ImageInfo> sourceImg = new List<ImageInfo>();
        public static List<ImageInfo> targetImg = new List<ImageInfo>();

        public static List<ImageInfo> Result = new List<ImageInfo>();
        public static List<ImageInfo> Result2 = new List<ImageInfo>();

        public static string BaseDirectory = System.AppDomain.CurrentDomain.BaseDirectory;

        public static int splitBlockSize = 25;

        //static string BigImageFile = @"C:\Users\YR\Desktop\test3.png";
        //static Image BigImage = Image.FromFile(BigImageFile);
        static Image BigImage;

        public static void Clear()
        {
            sourceImg.Clear();
            targetImg.Clear();
            Result.Clear(); 
        }

        public static bool IsImage1Loaded = false;
        public static bool IsImage2Loaded = false;

        public static void Init()
        {
            IsImage1Loaded = false;
            IsImage2Loaded = false;
            Clear();
            DeleteDirectory();
            BigImage = ImageHelper.CaptureImage(ImageHelper.GetFullScreen(), 448, 156, 1024, 768); ;
        }


        static void DeleteDirectory()
        {
            if (Directory.Exists("SourceImages"))
            {
                DirectoryInfo di = new DirectoryInfo(BaseDirectory + "SourceImages");
                di.Delete(true);
            }
            if (Directory.Exists("TargetImages"))
            {
                DirectoryInfo di = new DirectoryInfo(BaseDirectory + "TargetImages");
                di.Delete(true);
            }
        }

        public static void LoadImage1()
        {
            if (!Directory.Exists("SourceImages"))
                Directory.CreateDirectory("SourceImages");

            Image sImage = ImageHelper.CaptureImage(BigImage, 93, 312, 380, 285);

            int w_Block = (int)Math.Ceiling(sImage.Width / Convert.ToDouble(splitBlockSize));
            int h_Block = (int)Math.Ceiling(sImage.Height / Convert.ToDouble(splitBlockSize));
            for (int i = 0; i < w_Block; i++)
            {
                for (int j = 0; j < h_Block; j++)
                {
                    int CropWidth = splitBlockSize;
                    int CropHeight = splitBlockSize;
                    if ((i + 1) * splitBlockSize > sImage.Width)
                    {
                        CropWidth = sImage.Width - i * splitBlockSize;
                    }
                    if ((j + 1) * splitBlockSize > sImage.Height)
                    {
                        CropHeight = sImage.Height - j * splitBlockSize;
                    }
                    var imgageBlock = ImageHelper.CaptureImage(sImage, i * splitBlockSize, j * splitBlockSize, CropWidth, CropHeight);
                    var picFileName = BaseDirectory + "SourceImages\\" + i.ToString().PadLeft(2, '0') + j.ToString().PadLeft(2, '0') + ".png";
                    imgageBlock.Save(picFileName);
                    sourceImg.Add(new ImageInfo() { sFileName = picFileName, X = i * splitBlockSize, Y = j * splitBlockSize });
                }
            }

            IsImage1Loaded = true;
        }

        public static void LoadImage2()
        {
            if (!Directory.Exists("TargetImages"))
                Directory.CreateDirectory("TargetImages");

            Image tImage = ImageHelper.CaptureImage(BigImage, 550, 312, 380, 285);

            int w_Block = (int)Math.Ceiling(tImage.Width / Convert.ToDouble(splitBlockSize));
            int h_Block = (int)Math.Ceiling(tImage.Height / Convert.ToDouble(splitBlockSize));

            for (int i = 0; i < w_Block; i++)
            {
                for (int j = 0; j < h_Block; j++)
                {
                    int CropWidth = splitBlockSize;
                    int CropHeight = splitBlockSize;
                    if ((i + 1) * splitBlockSize > tImage.Width)
                    {
                        CropWidth = tImage.Width - i * splitBlockSize;
                    }
                    if ((j + 1) * splitBlockSize > tImage.Height)
                    {
                        CropHeight = tImage.Height - j * splitBlockSize;
                    }
                    var imgageBlock = ImageHelper.CaptureImage(tImage, i * splitBlockSize, j * splitBlockSize, CropWidth, CropHeight);
                    var picFileName = BaseDirectory + "TargetImages\\" + i.ToString().PadLeft(2, '0') + j.ToString().PadLeft(2, '0') + ".png";
                    imgageBlock.Save(picFileName);
                    targetImg.Add(new ImageInfo() { tFileName = picFileName, X = i * splitBlockSize, Y = j * splitBlockSize });
                }
            }

            IsImage2Loaded = true;
        }


        public static void Compare()
        {
            for (int i = 0; i < sourceImg.Count; i++)
            {
                int index = i;
                //Task.Run(() =>
                //{
                    try
                    {
                        var rect = EmguCvHelper.MatchPictureSimilarity(sourceImg[index].sFileName, targetImg[index].tFileName, out double r, Emgu.CV.CvEnum.ImreadModes.Unchanged);
                        Result.Add(
                            new ImageInfo()
                            { 
                                sFileName= sourceImg[index].sFileName,
                                tFileName= targetImg[index].tFileName,
                                Similarity = r,
                                X = sourceImg[index].X + 93,
                                Y = sourceImg[index].Y + 312
                            }
                        );
                    }
                    catch(Exception e)
                    {
                        throw e;
                    }
                //});
            }
            Result2.Clear();
            //for (int i = 0; i < Result.Count; i++)
            //{
            //    if (Result[i].Similarity < 0.995)
            //    {
            //        var rect = EmguCvHelper.MatchPictureSimilarity(Result[i].sFileName, Result[i].tFileName, out double r, Emgu.CV.CvEnum.ImreadModes.Unchanged);
            //        Result2.Add(new ImageInfo() { Similarity = r, X = Result[i].X, Y = Result[i].Y });
            //    }
            //}
        }




    }

    public class ImageInfo
    {
        public string sFileName { get; set; }

        public string tFileName { get; set; }

        public double Similarity { get; set; }

        public int X { get; set; }
        public int Y { get; set; }
    }
}
