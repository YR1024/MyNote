using System.Diagnostics;

namespace VideoWeb.Helper
{
    public class VideoHelper
    {
        static string FFmpegpath = AppDomain.CurrentDomain.BaseDirectory + "Helper\\";
        //static string FFmpegpath = AppDomain.CurrentDomain.BaseDirectory;

        /// <summary>
        /// 视频切割
        /// </summary>
        /// <param name="OriginFile">视频源文件</param>
        /// <param name="DstFile">生成的文件</param>
        /// <param name="startTime">开始时间</param>
        /// <param name="endTime">结束时间</param>
        /// <returns></returns>
        public static string Cut(string OriginFile, string DstFile, TimeSpan startTime, TimeSpan endTime)
        {
            string strCmd = "-ss 00:00:10 -i " + OriginFile + " -ss " +
            startTime.ToString() + " -t " + endTime.ToString() + " -vcodec copy " + DstFile + " -y ";

            //string strCmd = $"ffmpeg - i {OriginFile} - ss {startTime.ToString()} - to {endTime.ToString()} - c:v copy -c:a copy {DstFile}";

            {
                System.Diagnostics.ProcessStartInfo cmd_StartInfo = new System.Diagnostics.ProcessStartInfo();
                cmd_StartInfo.FileName = FFmpegpath + "ffmpeg.exe";//要执行的程序名称
                if (!System.IO.File.Exists(cmd_StartInfo.FileName))
                {
                    return DstFile;
                }
                cmd_StartInfo.Arguments = " " + strCmd;
                cmd_StartInfo.RedirectStandardError = false;
                cmd_StartInfo.RedirectStandardOutput = false;
                cmd_StartInfo.UseShellExecute = true;
                cmd_StartInfo.CreateNoWindow = false;
                System.Diagnostics.Process cmd = new System.Diagnostics.Process();
                cmd.StartInfo = cmd_StartInfo;
                try
                {
                    cmd.Start();

                }
                catch (Exception ex)
                {

                }
                //cmd.WaitForExit();//等待程序执行完退出进程
            }

            //System.Diagnostics.Process p = new System.Diagnostics.Process();
            //p.StartInfo.FileName = FFmpegpath +"ffmpeg.exe";//要执行的程序名称
            //p.StartInfo.Arguments = strCmd;
            //p.StartInfo.UseShellExecute = false;
            //p.StartInfo.RedirectStandardInput = false;//可能接受来自调用程序的输入信息
            //p.StartInfo.RedirectStandardOutput = false;//由调用程序获取输出信息
            //p.StartInfo.RedirectStandardError = false;//重定向标准错误输出
            //p.StartInfo.CreateNoWindow = false;//不显示程序窗口

            //p.Start();//启动程序
            //p.WaitForExit();//等待程序执行完退出进程



            if (System.IO.File.Exists(DstFile))
            {
                return DstFile;
            }
            return "";
        }


        //视频合并
        public async static Task<bool> Combine(string File1, string File2, string DstFile)
        {
            return await Task.Run(() =>
            {
                string strTmp1 = File1 + ".ts";
                string strTmp2 = File2 + ".ts";
                try
                {

                    #region 转换文件类型
                    string strCmd1 = " -i " + File1 + " -c copy -bsf:v h264_mp4toannexb -f mpegts " + strTmp1 + " -y ";
                    string strCmd2 = " -i " + File2 + " -c copy -bsf:v h264_mp4toannexb -f mpegts " + strTmp2 + " -y ";

                    //转换文件类型，由于不是所有类型的视频文件都支持直接合并，需要先转换格式
                    System.Diagnostics.Process p = new System.Diagnostics.Process();
                    p.StartInfo.FileName = FFmpegpath + "ffmpeg.exe"; ;//要执行的程序名称
                    p.StartInfo.Arguments = " " + strCmd1;
                    p.StartInfo.UseShellExecute = false;
                    //p.StartInfo.WorkingDirectory = FFmpegpath;
                    p.StartInfo.RedirectStandardInput = false;//可能接受来自调用程序的输入信息
                    p.StartInfo.RedirectStandardOutput = false;//由调用程序获取输出信息
                    p.StartInfo.RedirectStandardError = false;//重定向标准错误输出
                    p.StartInfo.CreateNoWindow = false;//不显示程序窗口
                    p.Start();//启动程序
                    p.WaitForExit();

                    //转换文件类型，由于不是所有类型的视频文件都支持直接合并，需要先转换格式
                    p = new System.Diagnostics.Process();
                    p.StartInfo.FileName = FFmpegpath + "ffmpeg.exe"; ;//要执行的程序名称
                    p.StartInfo.Arguments = " " + strCmd2;
                    p.StartInfo.UseShellExecute = false;
                    p.StartInfo.RedirectStandardInput = false;//可能接受来自调用程序的输入信息
                    p.StartInfo.RedirectStandardOutput = false;//由调用程序获取输出信息
                    p.StartInfo.RedirectStandardError = false;//重定向标准错误输出
                    p.StartInfo.CreateNoWindow = false;//不显示程序窗口
                    p.Start();//启动程序
                    p.WaitForExit();

                    #endregion

                    string strCmd = " -i \"concat:" + strTmp1 + "|" +
                                    strTmp2 + "\" -c copy -bsf:a aac_adtstoasc -movflags +faststart " + DstFile + " -y ";
                    //string strCmd = " -i \"concat:" + File1 + "|" + File2 +
                    //                "\" -c copy -bsf:a aac_adtstoasc -movflags +faststart " + DstFile + " -y ";
                    //合并
                    p = new System.Diagnostics.Process();
                    p.StartInfo.FileName = FFmpegpath + "ffmpeg.exe"; ;//要执行的程序名称
                    p.StartInfo.Arguments = " " + strCmd;
                    p.StartInfo.UseShellExecute = false;
                    //p.StartInfo.WorkingDirectory = Directory.GetCurrentDirectory();
                    p.StartInfo.RedirectStandardInput = false;//可能接受来自调用程序的输入信息
                    p.StartInfo.RedirectStandardOutput = false;//由调用程序获取输出信息
                    p.StartInfo.RedirectStandardError = false;//重定向标准错误输出
                    p.StartInfo.CreateNoWindow = false;//不显示程序窗口

                    p.Start();//启动程序

                    //向CMD窗口发送输入信息：
                    // p.StandardInput.Write("ipconfig");

                    //string output = p.StandardOutput.ReadToEnd();
                    p.WaitForExit();//等待程序执行完退出进程
                                    //-ss表示搜索到指定的时间 -i表示输入的文件 -y表示覆盖输出 -f表示强制使用的格式

                    if (System.IO.File.Exists(DstFile))
                    {
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
                catch (Exception e)
                {
                    return false;
                }
                finally
                {
                    if (File.Exists(strTmp1))
                    {
                        File.Delete(strTmp1);
                    }
                    if (File.Exists(strTmp2))
                    {
                        File.Delete(strTmp2);
                    }
                }
            });
        }


        public async static Task<bool> MergeVideo(string File1, string File2, string DstFile)
        {
            bool result = false;
            string MergeVideoPath = AppDomain.CurrentDomain.BaseDirectory + "MergeVideo.exe";
            return await Task.Run(() =>
            {
                // 设置调用参数和启动信息
                ProcessStartInfo psi = new ProcessStartInfo
                {
                    FileName = MergeVideoPath, // 替换为你的控制台程序的实际文件名
                    Arguments = $"\"{File1}\" \"{File2}\" \"{DstFile}\"", // 使用引号括起参数
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                // 启动进程并获取输出
                using (Process process = new Process())
                {
                    process.StartInfo = psi;
                    process.Start();
                    string output = process.StandardOutput.ReadToEnd();
                    process.WaitForExit();

                    // 输出控制台程序返回的结果
                    //Console.WriteLine(output);
                    result = Boolean.Parse(output);
                }
                return result;
            });
        }


        public static string FindLongestCommonSubstring(string str1, string str2)
        {
            int[,] dp = new int[str1.Length + 1, str2.Length + 1];
            int maxLength = 0;
            int endIndex = 0;

            for (int i = 1; i <= str1.Length; i++)
            {
                for (int j = 1; j <= str2.Length; j++)
                {
                    if (str1[i - 1] == str2[j - 1])
                    {
                        dp[i, j] = dp[i - 1, j - 1] + 1;
                        if (dp[i, j] > maxLength)
                        {
                            maxLength = dp[i, j];
                            endIndex = i - 1;
                        }
                    }
                    else
                    {
                        dp[i, j] = 0;
                    }
                }
            }

            return str1.Substring(endIndex - maxLength + 1, maxLength);
        }
    }

}
