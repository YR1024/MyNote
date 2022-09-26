using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using static DesktopIcon.Helper;

namespace DesktopIcon
{
    public partial class Form1 : Form
    {
        public Form1()
        {

            InitializeComponent();
            Visible = false;
            Helper.StartUp();

            this.ShowInTaskbar = false;
            this.WindowState = FormWindowState.Minimized;
            MouseMoveTask();
            MousePointChanged += MousePointChange;
        }


        public static bool ShowDesktopIcon = false;
        public POINT CurrentMousePoint = new POINT(0, 0);
        void MouseMoveTask()
        {
            Task.Run(() => {
                while (true)
                {
                    Helper.GetCursorPos(out POINT lpPoint);
                    if(lpPoint.X != CurrentMousePoint.X || lpPoint.Y != CurrentMousePoint.Y)
                    {
                        MousePointChanged();
                        CurrentMousePoint = lpPoint;
                    }
                    Thread.Sleep(50);
                }
            });


            Task t = new Task(() =>
            {

                while (true)
                {
                    if (curTimes >= Times)
                    {
                        continue;
                    }
                    while (curTimes++ < Times) //计时
                    {
                        Thread.Sleep(1000);
                    }
                    ShowDesktopIcon = false;
                    ShowHiddenIcon(ShowDesktopIcon); //隐藏
                    if (IsHiddenTaskBar)
                    {
                        ShowTaskbar(ShowDesktopIcon);
                    }
                    Thread.Sleep(30);
                }

            });
            t.Start();
        }

        int curTimes = 0;
        readonly int Times = 10; 
        bool AlwaysShowIcon = false;
        bool IsHiddenTaskBar = false;

        public void MousePointChange()
        {
            if (AlwaysShowIcon)
            {
                curTimes = 9999999;
            }
            else
            {
                curTimes = 0;
            }
            if (ShowDesktopIcon == false) //如果是隐藏则立马显现，如果是显示则不做操作
            {
                ShowDesktopIcon = true;
                ShowHiddenIcon(ShowDesktopIcon);//显示
                if (IsHiddenTaskBar)
                {
                    ShowTaskbar(ShowDesktopIcon);
                }
            }
        }


        public Action MousePointChanged = delegate { };


        private static void ShowHiddenIcon(bool isShow)
        {

            // 遍历顶级窗口
            EnumWindows((hwnd, lParam) =>
            {
                // 找到第一个 WorkerW 窗口，此窗口中有子窗口 SHELLDLL_DefView，所以先找子窗口
                var shellDll = FindWindowEx(hwnd, IntPtr.Zero, "SHELLDLL_DefView", null);
                if (shellDll != IntPtr.Zero)
                {
                    // 找到当前第一个 WorkerW 窗口的，后一个窗口，及第二个 WorkerW 窗口。
                    //IntPtr tempHwnd = Win32Func.FindWindowEx(IntPtr.Zero, hwnd, "WorkerW", null);

                    IntPtr Idesk1 = FindWindowEx(shellDll, IntPtr.Zero, "SysListView32", "FolderView"); //获取桌面 


                    //Win32Func.ShowWindow(Idesk, b % 5);  //value=5时显示，value=0时隐藏
                    int value = isShow ? 5 : 0;
                    ShowWindow(Idesk1, value);  //value=5时显示，value=0时隐藏
                }
                return true;
            }, IntPtr.Zero.ToInt32());

        }

        /// summary 
        /// 隐藏任务栏和桌面图标
        /// /summary 
        private static void ShowTaskbar(bool Taskbar)
        {
            IntPtr trayHwnd = FindWindow("Shell_TrayWnd", null);
            if (trayHwnd != IntPtr.Zero)
            {
                //ShowWindow(desktopPtr, );//隐藏桌面图标 （0是隐藏，1是显示）
                ShowWindow(trayHwnd, Taskbar ? 1 : 0);//隐藏任务栏（0是隐藏，1是显示）
                //ShowWindow(hStar, );//隐藏windows 按钮
            }
        }

      


        [DllImport("User32.dll", EntryPoint = "FindWindow")]
        public extern static IntPtr FindWindow(string lpClassName, string lpWindowName);

        [DllImport("user32.dll", EntryPoint = "ShowWindow", CharSet = CharSet.Auto)]
        public static extern int ShowWindow(IntPtr hwnd, int nCmdShow);

        [DllImport("user32.dll", EntryPoint = "FindWindowEx", SetLastError = true)]
        static extern IntPtr FindWindowEx(IntPtr hwndParent, IntPtr hwndChildAfter, string lpszClass, string lpszWindow);
      

        public delegate bool EnumWindowsCallback(IntPtr hwnd, int lParam);
        [DllImport("user32.dll")]
        private static extern int EnumWindows(EnumWindowsCallback callPtr, int lParam);




        private void 运行ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (((System.Windows.Forms.ToolStripMenuItem)sender).Checked)
            {
                AlwaysShowIcon = false;

            }
            else
            {
                AlwaysShowIcon = true;

            }
        }


        private void 隐藏任务栏ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (((System.Windows.Forms.ToolStripMenuItem)sender).Checked)
            {
                IsHiddenTaskBar = true;
            }
            else
            {
                IsHiddenTaskBar = false;
            }
        }

        private void 开机启动ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (((System.Windows.Forms.ToolStripMenuItem)sender).Checked)
            {
                Helper.StartUp();
            }
            else
            {
                Helper.CancelStartUp();
            }
        }

        private void 关闭ToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.Dispose();
            this.Close();
        }

    }

    public class Helper
    {
        [DllImport("user32.dll")]
        public static extern bool GetCursorPos(out POINT lpPoint);

        [StructLayout(LayoutKind.Sequential)]
        public struct POINT
        {
            public int X;
            public int Y;
            public POINT(int x, int y)
            {
                this.X = x;
                this.Y = y;
            }
        }



        public static void StartUp()
        {
            //获取程序执行路径..
            string starupPath = AppDomain.CurrentDomain.BaseDirectory + "DesktopIcon.exe";
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


        public static void CancelStartUp()
        {
            //获取程序执行路径..
            string starupPath = AppDomain.CurrentDomain.BaseDirectory + "DesktopIcon.exe";
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
    }

}
