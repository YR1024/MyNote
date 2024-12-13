using Microsoft.Win32;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace FrpClientManager
{
    internal class Program
    {

        public static bool isShowCmdWnd = false;
        private static Mutex mutex;
        static void Main(string[] args)
        {

            try
            {
                //单例程序
                if (SingleProcess())
                    return;

                //开机启动
                StartUp();

                //隐藏控制台窗口
                Console.Title = "FrpClientManager";
                ShowWindow(FindWindow(null, "FrpClientManager"), 0);

                Start();
                Console.ReadLine();
            }
            catch (Exception ex)
            {
                Logger.Instance.WriteLog("[Main]:" + ex.Message);
            }
          
        }


        private static void CmdProcess_ErrorDataReceived(object sender, DataReceivedEventArgs e)
        {
            if (!string.IsNullOrEmpty(e.Data))
            {
                Console.WriteLine("Error: " + e.Data); // 将错误输出打印到控制台
                Logger.Instance.WriteLog(e.Data);
            }
        }

        private static void CmdProcess_OutputDataReceived(object sender, DataReceivedEventArgs e)
        {
            Console.WriteLine(e.Data); // 将输出打印到控制台
            Logger.Instance.WriteLog(e.Data);
        }


        static Task Start()
        {
            return Task.Run(async() =>
            {
                while (true)
                {
                    try
                    {
                        // 需要执行的CMD命令，注意使用"/c"参数，这样cmd执行完命令后会自动关闭窗口 /k不关闭窗口
                        string cmdCommand = "/c frpc.exe -c frpc.ini";
                        //string cmdCommand = "/k dir";

                        // 创建一个新的进程
                        Process cmdProcess = new Process();

                        // 设置进程启动信息
                        cmdProcess.StartInfo.FileName = "cmd.exe"; // 设置进程要启动的程序
                        cmdProcess.StartInfo.Arguments = cmdCommand; // 设置需要执行的命令
                        cmdProcess.StartInfo.WorkingDirectory = AppDomain.CurrentDomain.BaseDirectory; // 设置工作目录（不设置的话，开机自启会报错）
                        cmdProcess.StartInfo.UseShellExecute = isShowCmdWnd; // 不使用系统外壳程序启动
                        cmdProcess.StartInfo.RedirectStandardOutput = !isShowCmdWnd; // 重定向输出，这样就可以捕获输出内容
                        cmdProcess.StartInfo.RedirectStandardError = !isShowCmdWnd; // 重定向错误
                        cmdProcess.StartInfo.CreateNoWindow = !isShowCmdWnd; // 不创建新窗口
                                                                             // 注册事件处理程序以异步读取输出
                        cmdProcess.OutputDataReceived += CmdProcess_OutputDataReceived;
                        // 注册事件处理程序以异步读取错误输出
                        cmdProcess.ErrorDataReceived += CmdProcess_ErrorDataReceived;


                        // 启动进程
                        cmdProcess.Start();

                        // 读取输出信息
                        //string output = cmdProcess.StandardOutput.ReadToEnd();
                        //// 读取错误信息（如果有）
                        //string error = cmdProcess.StandardError.ReadToEnd();

                        if (!isShowCmdWnd)
                        {
                            // 开始异步读取输出和错误输出
                            cmdProcess.BeginOutputReadLine();
                            cmdProcess.BeginErrorReadLine();
                        }

                        // 等待进程退出
                        cmdProcess.WaitForExit();
                        cmdProcess.OutputDataReceived -= CmdProcess_OutputDataReceived;
                        cmdProcess.ErrorDataReceived -= CmdProcess_ErrorDataReceived;
                    }
                    catch(Exception ex)
                    {
                        Logger.Instance.WriteLog("[Start]:" + ex.StackTrace);
                        Logger.Instance.WriteLog("[Start]:" + ex.Message);
                    }
                    await Task.Delay(10_000);
                }

            });
        }

        #region Helper
        [DllImport("user32.dll", EntryPoint = "ShowWindow", SetLastError = true)]
        static extern bool ShowWindow(IntPtr hWnd, uint nCmdShow);
        [DllImport("user32.dll", EntryPoint = "FindWindow", SetLastError = true)]
        public static extern IntPtr FindWindow(string lpClassName, string lpWindowName);


        /// <summary>
        /// 开机启动
        /// </summary>
        private static void StartUp()
        {
            //获取程序执行路径..
            string starupPath = AppDomain.CurrentDomain.BaseDirectory + $"{Assembly.GetExecutingAssembly().GetName().Name}.exe";
            //class Micosoft.Win32.RegistryKey. 表示Window注册表中项级节点,此类是注册表装.
            //RegistryKey loca = Registry.LocalMachine;
            RegistryKey loca = Registry.CurrentUser;
            RegistryKey run = loca.CreateSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run");

            try
            {
                //SetValue:存储值的名称
                run.SetValue("StartFrpClient", starupPath);
                loca.Close();
            }
            catch (Exception ee)
            {
                throw ee;
            }

        }

        /// <summary>
        /// 单例程序
        /// </summary>
        /// <returns></returns>
        static bool SingleProcess()
        {
            mutex = new Mutex(true, "FrpClientMutex");
            if (!mutex.WaitOne(0, false))
            {
                return true;
            }
            return false;
        }
        #endregion


    }
}
