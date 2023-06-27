using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Resources;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using WindowTop.Properties;
using static System.Net.Mime.MediaTypeNames;
using Application = System.Windows.Forms.Application;

namespace WindowTop
{
    internal class Program
    {
        [DllImport("user32.dll")]
        static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);


        [DllImport("user32.dll", EntryPoint = "ShowWindow", SetLastError = true)]
        static extern bool ShowWindow(IntPtr hWnd, uint nCmdShow);
        [DllImport("user32.dll", EntryPoint = "FindWindow", SetLastError = true)]
        public static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        private static Mutex mutex;

        [STAThread]
        static void Main(string[] args)
        {
            //单例程序
            if (SingleProcess())
                return;

            Console.Title = "WindowsTop";
            while (!ShowWindow(FindWindow(null, "WindowsTop"), 0))
            {

            }

            Helper.LoadConfig();
            _config = Helper.config;
            if (_config.StartUp)
            {
#if DEBUG
                Helper.StartUp();
#endif
            }

            // 创建托盘图标
            var notifyIcon = new NotifyIcon();
            notifyIcon.Icon = Resources.windowtop;
            notifyIcon.Text = "Window TopMost";
            notifyIcon.Visible = true;

            // 设置托盘图标的快捷菜单
            var contextMenu = new ContextMenu();
            var menuItemSet = new MenuItem("设置");
            menuItemSet.Click += MenuItemSet_Click;
            contextMenu.MenuItems.Add(menuItemSet);

            var menuItemStartUp = new MenuItem("开机启动");
            menuItemStartUp.Checked = _config.StartUp;
            menuItemStartUp.Click += MenuItemStartUp_Click; ;
            contextMenu.MenuItems.Add(menuItemStartUp);


            var menuItemExit = new MenuItem("退出");
            menuItemExit.Click += (s, e) => Application.Exit();
            contextMenu.MenuItems.Add(menuItemExit);
            notifyIcon.ContextMenu = contextMenu;


            Helper.SpiltCombinationKey(_config.ShortcutKeys);
            // 注册鼠标单击事件和快捷键
            //MouseHook.RegisterMouseClickEvent(MouseButtons.Left, HandleMouseClick);
            //KeyboardHook.RegisterHotKey(ModifierKeys.Control | ModifierKeys.Shift, Keys.T, HandleHotKey);
            keyId = HotKeyManager.RegisterHotKey(Keys.T, KeyModifiers.Control | KeyModifiers.Alt);
            HotKeyManager.HotKeyPressed += new EventHandler<HotKeyEventArgs>(HotKeyManager_HotKeyPressed);

            // 运行消息循环
            Application.Run();
            Application.ApplicationExit += Application_ApplicationExit;

            mutex.ReleaseMutex();
        }

     
        static Config _config;
     

        static bool SingleProcess()
        {
            mutex = new Mutex(true, "WindowsTop");
            if (!mutex.WaitOne(0, false))
            {
                return true;
            }
            return false;
        }

        /// <summary>
        /// 设置
        /// </summary>
        private static void MenuItemSet_Click(object sender, EventArgs e)
        {
            SettingForm settingForm = new SettingForm();
            if (settingForm.ShowDialog() == DialogResult.OK)
            {
                //_config.ShortcutKeys = ;
            }
            Helper.SaveConfig();
        }

        private static void MenuItemStartUp_Click(object sender, EventArgs e)
        {
            if (((System.Windows.Forms.ToolStripMenuItem)sender).Checked)
            {
                Helper.StartUp();
                _config.StartUp = true;
            }
            else
            {
                Helper.CancelStartUp();
                _config.StartUp = false;
            }
            Helper.SaveConfig();
        }

        static int keyId;
        private static void Application_ApplicationExit(object sender, EventArgs e)
        {
            HotKeyManager.UnregisterHotKey(keyId);
        }

        static void HotKeyManager_HotKeyPressed(object sender, HotKeyEventArgs e)
        {
            // 获取当前活动窗口句柄
            var hWnd = GetForegroundWindow();
            if (!WindowHWndList.Contains(hWnd))
            {
                TopMostWindow.SetTopomost(hWnd);
                WindowHWndList.Add(hWnd);
            }
            else
            {
                TopMostWindow.CancelTopomost(hWnd);
                WindowHWndList.Remove(hWnd);
            }

            //Console.WriteLine(hWnd.ToString());
            //MessageBox.Show(hWnd.ToString());
        }
         
        static List<IntPtr> WindowHWndList  = new List<IntPtr>();

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
                run.SetValue("WindowsTopTool", starupPath);
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
                run.DeleteValue("WindowsTopTool");
                loca.Close();
            }
            catch (Exception ee)
            {
                //throw ee;
            }
        }



        static PerformanceCounter cpuCounter = new PerformanceCounter("Processor Information", "% Processor Utility", "_Total");


        public static Config config;
        public static void SaveConfig()
        {
            Properties.Settings1.Default.StartUp = config.StartUp;
            Properties.Settings1.Default.ShortcutKeys = config.ShortcutKeys;
            Properties.Settings1.Default.Save();

        }


        public static void LoadConfig()
        {
            config = new Config()
            {
                StartUp = Properties.Settings1.Default.StartUp,
                ShortcutKeys = Properties.Settings1.Default.ShortcutKeys,
            };

        }


        public static void SpiltCombinationKey(Keys keys)
        {
            var a = keys.ToString();
            var keyList = a.Split(',');
            foreach (var key in keyList)
            {
                //var k = key as Keys;
                if(Enum.TryParse(key, out Keys _key))
                {
                        

                }

            }
            //foreach (var item in keys)
            //{

            //}
        }

    }

    [Serializable]
    public class Config
    {

        public static Config Instacnce = new Config();

        public bool StartUp { get; set; } = true;
        public Keys ShortcutKeys { get; set; }

    }

}
