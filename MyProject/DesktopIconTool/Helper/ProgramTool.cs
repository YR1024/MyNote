using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DesktopIconTool.Helper
{
    public class ProgramTool
    {
        /// <summary>
        /// 开机启动
        /// </summary>
        public static void StartUp()
        {
            //获取程序执行路径..
            string starupPath = AppDomain.CurrentDomain.BaseDirectory + Assembly.GetExecutingAssembly().GetName().Name + ".exe";
            //class Micosoft.Win32.RegistryKey. 表示Window注册表中项级节点,此类是注册表装.
            //RegistryKey loca = Registry.LocalMachine;
            RegistryKey loca = Registry.CurrentUser;
            RegistryKey run = loca.CreateSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run");

            try
            {
                //SetValue:存储值的名称
                run.SetValue("DesktopIconHidden", starupPath);
                loca.Close();
            }
            catch (Exception ee)
            {
                //throw ee;
            }
        }

        /// <summary>
        /// 取消开机启动
        /// </summary>
        public static void CancelStartUp()
        {
            //获取程序执行路径..
            string starupPath = AppDomain.CurrentDomain.BaseDirectory + Assembly.GetExecutingAssembly().GetName().Name + ".exe";
            //class Micosoft.Win32.RegistryKey. 表示Window注册表中项级节点,此类是注册表装.
            //RegistryKey loca = Registry.LocalMachine;
            RegistryKey loca = Registry.CurrentUser;
            RegistryKey run = loca.CreateSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Run");

            try
            {
                //SetValue:存储值的名称
                run.DeleteValue("DesktopIconHidden");
                loca.Close();
            }
            catch (Exception ee)
            {
                //throw ee;
            }
        }

        private static Mutex mutex;
        public static bool SingleProcess(string processFlag)
        {
            mutex = new Mutex(true, processFlag);
            if (!mutex.WaitOne(0, false))
            {
                return true;
            }
            return false;
        }

        public static void ReleaseSingleProcess()
        {
            mutex.ReleaseMutex();
        }



        static PerformanceCounter cpuCounter = new PerformanceCounter("Processor Information", "% Processor Utility", "_Total");
        /// <summary>
        /// 获取CPU使用率
        /// </summary>
        /// <returns></returns>
        public static int GetCpuUsage()
        {
            //PerformanceCounter cpuCounter;
            //PerformanceCounter ramCounter;
            //cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
            //cpuCounter = new PerformanceCounter("Processor Information", "% Processor Utility", "_Total");
            //ramCounter = new PerformanceCounter("Memory", "Available MBytes");
            //return ramCounter.NextValue() + "MB";
            return (int)cpuCounter.NextValue();
        }
    }
}
