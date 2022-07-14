using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace AutoQQSignIn
{
    public class Helper
    {

        //这个方法是根据软件名称获取软件路径，稍微修改一下就会得到获取所有软件的方法
        ///<summary>
        ///软件是否安转
        ///</summary>
        ///<param name="SoftWareName">软件名称</param>
        ///<param name="SoftWarePath">安装路径</param>
        ///<returns>true or false</returns>
        public static bool GetSoftWare(string SoftWareName, out string SoftWarePath)
        {
            SoftWarePath = string.Empty;
            List<RegistryKey> RegistryKeys = new List<RegistryKey>();
            RegistryKeys.Add(Registry.ClassesRoot);
            RegistryKeys.Add(Registry.CurrentConfig);
            RegistryKeys.Add(Registry.CurrentUser);
            RegistryKeys.Add(Registry.LocalMachine);
            RegistryKeys.Add(Registry.PerformanceData);
            RegistryKeys.Add(Registry.Users);
            Dictionary<string, string> Softwares = new Dictionary<string, string>();
            string SubKeyName = @"Software\Microsoft\Windows\CurrentVersion\Uninstall";
            foreach (RegistryKey Registrykey in RegistryKeys)
            {
                using (RegistryKey RegistryKey1 = Registrykey.OpenSubKey(SubKeyName, false))
                {
                    if (RegistryKey1 == null)//判断对象不存在
                        continue;
                    if (RegistryKey1.GetSubKeyNames() == null)
                        continue;
                    string[] KeyNames = RegistryKey1.GetSubKeyNames();
                    foreach (string KeyName in KeyNames)//遍历子项名称的字符串数组
                    {
                        using (RegistryKey RegistryKey2 = RegistryKey1.OpenSubKey(KeyName,
                        false))//遍历子项节点
                        {
                            if (RegistryKey2 == null)
                                continue;
                            string SoftwareName = RegistryKey2.GetValue("DisplayName",
                            "").ToString();//获取软件名
                            string InstallLocation = RegistryKey2.GetValue("InstallLocation",
                            "").ToString();//获取安装路径
                            if (!string.IsNullOrEmpty(InstallLocation)
                            && !string.IsNullOrEmpty(SoftwareName))
                            {
                                if (!Softwares.ContainsKey(SoftwareName))
                                    Softwares.Add(SoftwareName, InstallLocation);
                            }
                        }
                    }
                }
            }
            if (Softwares.Count <= 0)
                return false;
            foreach (string SoftwareName in Softwares.Keys)
            {
                if (SoftwareName.Contains(SoftWareName))
                {
                    SoftWarePath = Softwares[SoftwareName];
                    return true;
                }
            }
            return false;
        }


        public static void StartProcess(string exeFile)
        {
            Process.Start(exeFile);//否则启动进程
        }

        public static void CloseProcess(string exeFile)
        {

        }


        //这个方法是修改之后的
        ///<summary>
        ///本机所有安转软件
        ///</summary>
        ///<returns>软件列表：软件名称，安转路径</returns>
        private static Dictionary<string, string> GetSoftWares()
        {
            List<RegistryKey> RegistryKeys = new List<RegistryKey>();
            RegistryKeys.Add(Registry.ClassesRoot);
            RegistryKeys.Add(Registry.CurrentConfig);
            RegistryKeys.Add(Registry.CurrentUser);
            RegistryKeys.Add(Registry.LocalMachine);
            RegistryKeys.Add(Registry.PerformanceData);
            RegistryKeys.Add(Registry.Users);
            Dictionary<string, string> Softwares = new Dictionary<string, string>();
            string SubKeyName = @"Software\Microsoft\Windows\CurrentVersion\Uninstall";
            foreach (RegistryKey Registrykey in RegistryKeys)
            {
                using (RegistryKey RegistryKey1 = Registrykey.OpenSubKey(SubKeyName, false))
                {
                    if (RegistryKey1 == null)//判断对象不存在
                        continue;
                    if (RegistryKey1.GetSubKeyNames() == null)
                        continue;
                    string[] KeyNames = RegistryKey1.GetSubKeyNames();
                    foreach (string KeyName in KeyNames)//遍历子项名称的字符串数组
                    {
                        using (RegistryKey RegistryKey2 = RegistryKey1.OpenSubKey(KeyName,
                        false))//遍历子项节点
                        {
                            if (RegistryKey2 == null)
                                continue;
                            string SoftwareName = RegistryKey2.GetValue("DisplayName",
                            "").ToString();//获取软件名
                            string InstallLocation = RegistryKey2.GetValue("InstallLocation",
                            "").ToString();//获取安装路径
                            if (!string.IsNullOrEmpty(InstallLocation)
                            && !string.IsNullOrEmpty(SoftwareName))
                            {
                                if (!Softwares.ContainsKey(SoftwareName))
                                    Softwares.Add(SoftwareName, InstallLocation);
                            }
                        }
                    }
                }
            }
            return Softwares;
        }
        //        这样就可以返回这个的安装列表，如果想获取更多安装信息的话添加即可。
        //Registry.ClassesRoot 对应注册表HKEY_CLASSES_ROOT
        //Registry.CurrentConfig 对应注册表HKEY_CURRENT_CONFIG
        //Registry.CurrentUser 对应注册表HKEY_CURRENT_USER
        //Registry.LocalMachine 对应注册表HKEY_LOCAL_MACHINE
        //Registry.PerformanceData 对应注册表 HKEY_PERFORMANCE_DATA
        //Registry.Users           对应注册表HKEY_USERS
        //由于安装的路径及账号的问题软件的同一个软件可能在不同的电脑上写在了不同的注册表中，因此要全
        //部遍历才可以获取全部软件的安装信息

    }
}



