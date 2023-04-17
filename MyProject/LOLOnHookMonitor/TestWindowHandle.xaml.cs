using AutomationServices.EmguCv.Helper;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using static Emgu.CV.Fuzzy.FuzzyInvoke;

namespace LOLOnHookMonitor
{
    /// <summary>
    /// TestWindowHandle.xaml 的交互逻辑
    /// </summary>
    public partial class TestWindowHandle : Window
    {
        public TestWindowHandle()
        {
            Funtion1();
            InitializeComponent();
        }


        void Funtion1()
        {
            var QQPCTrays = Process.GetProcessesByName("QQPCTray");
            var a = QQPCTrays[0].MainWindowHandle;
            var a1 = QQPCTrays[0].Handle;
            var B = QQPCTrays[1].MainWindowHandle;
            var B1 = QQPCTrays[1].Handle;

            var lolHunterIntPtr = Win32Helper.FindWindow(null, "腾讯电脑管家");
            var ctrlIntPtrs = Win32Helper.EnumChildWindowsCallback(lolHunterIntPtr);

            var startBtnIp = ctrlIntPtrs.Where(i => i.szClassName == "WindowsForms10.Button.app.0.33c0d9d_r3_ad1").LastOrDefault();

            if (startBtnIp.hWnd == IntPtr.Zero)
                return;
            //const int WM_CLICK = 0x00F5;
            //Win32Helper.SendMessage(startBtnIp.hWnd, WM_CLICK, IntPtr.Zero, IntPtr.Zero);

            //var startBtnIp2 = ctrlIntPtrs.Where(i => i.szWindowName == "启动").LastOrDefault();

            if (startBtnIp.szWindowName == "启动")
            {
                Win32Helper.SendClick(startBtnIp.hWnd);
                Thread.Sleep(1500);
                Win32Helper.SendClick(startBtnIp.hWnd);
            }
        }
    }
}
