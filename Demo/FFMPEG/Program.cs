using FFMpegCore;
using FFMpegCore.Enums;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FFMPEG
{
    class Program 
    { 
        /// <summary>
        /// 设置ffmpeg.exe的路径
        /// </summary>
        static string FFmpegPath = @"F:\github\MyNote\FFMPEG\ffmpeg.exe";

        static void Main(string[] args)
        {
            //string videoUrl = @"C:\Users\YR\Desktop\Snoop.mp4";
            //string targetUrl = @"C:\Users\YR\Desktop\Snoop.avi";

            ////视频转码
            //string para = string.Format("-i {0} -b 1024k -acodec copy -f avi {1}", videoUrl, targetUrl);
            //RunMyProcess(para);

            aa();

  
            Console.WriteLine("完成！");
            Console.ReadKey();
        }


        static void aa()
        {
            string inputPath = @"C:\Users\YR\Desktop\Snoop.mp4";
            string outputPath = @"C:\Users\YR\Desktop\Snoop.avi";


            var mediaInfo = FFProbe.Analyse(inputPath);


            FFMpegArguments.FromFileInput(inputPath).OutputToFile(outputPath, false, options => options
                                                    .WithVideoCodec(VideoCodec.LibX264)
                                                    .WithConstantRateFactor(21)
                                                    .WithAudioCodec(AudioCodec.Aac)
                                                    .WithVariableBitrate(4)
                                                    .WithVideoFilters(filterOptions => filterOptions.Scale(VideoSize.Hd))
                                                    .WithFastStart())
                                    .ProcessSynchronously();
        }


        static void RunMyProcess(string Parameters)
        {
            var p = new Process();
            p.StartInfo.FileName = FFmpegPath;
            p.StartInfo.Arguments = Parameters;
            //是否使用操作系统shell启动
            p.StartInfo.UseShellExecute = false;
            //不显示程序窗口
            p.StartInfo.CreateNoWindow = true;
            p.Start();
            Console.WriteLine("\n开始转码...\n");
            p.WaitForExit();
            p.Close();
        }

    }
}
