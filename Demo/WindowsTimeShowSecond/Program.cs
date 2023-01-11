using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WindowsTimeShowSecond
{
    internal class Program
    {
        static void Main(string[] args)
        {
            ShowSecond();
        }

        public static void ShowSecond()
        {
            //获取程序执行路径..
            ////string starupPath = AppDomain.CurrentDomain.BaseDirectory + "DesktopIconTool.exe";
            //class Micosoft.Win32.RegistryKey. 表示Window注册表中项级节点,此类是注册表装.
            //RegistryKey loca = Registry.LocalMachine;
            RegistryKey loca = Registry.CurrentUser;
            RegistryKey run = loca.CreateSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\Advanced");

            try
            {
                //SetValue:存储值的名称
                run.SetValue("ShowSecondsInSystemClock", 1, RegistryValueKind.DWord);
                loca.Close();
            }
            catch (Exception ee)
            {
                //throw ee;
            }
        }
    }
}
