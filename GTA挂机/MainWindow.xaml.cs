using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace GTA挂机
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }
         

        public Task t;
        public bool start = true;


        private void Button_Click(object sender, RoutedEventArgs e)
        {
            
            if(btn.Content.ToString() == "开始")
            {
                btn.Content = "停止";
                start = true;
                t = HangUpTask2();
                t.Start();
            }
            else
            {
                btn.IsEnabled = false;
                start = false;
                while (true)
                {
                    if (t.Status == TaskStatus.RanToCompletion)
                        break;
                    Thread.Sleep(300);
                }
                btn.Content = "开始";
                btn.IsEnabled = true;

            }

        }

        Task HangUpTask2()
        {
            return new Task(new Action(() => {
                Random rd = new Random();
                while (start)
                {
                    int r = rd.Next(3, 8);
                    //this.Dispatcher.Invoke(new Action(()=> {
                        switch (r)
                        {
                            case 3:
                                //txtbox.Focus();
                                WinIO.KeyDownUp(Keys.W);
                                break;
                            case 4:
                                //txtbox.Focus();

                                WinIO.KeyDownUp(Keys.S);
                                break;
                            case 5:
                                //txtbox.Focus();

                                WinIO.KeyDownUp(Keys.A);
                                break;
                            case 6:
                                //txtbox.Focus();

                                WinIO.KeyDownUp(Keys.D);
                                break;
                            case 7:
                                //txtbox.Focus();

                                WinIO.KeyDownUp(Keys.Space);
                                break;
                            default:
                                break;
                        }
                    //}));
                    
                    Thread.Sleep(r * 1000);
                }
            }));
        }



        #region function 2
       
        private void Button2_Click(object sender, RoutedEventArgs e)
        {
            //WinIO.KeyDownUp(Keys.W);

            Task.Run(new Action(() =>
            {
                 while (start)
                 {
                    this.Dispatcher.Invoke(new Action(() =>
                    {
                        txtbox.Focus();
                    }));
                    WinIO.KeyDownUp(Keys.W);

                    Thread.Sleep(2 * 1000);
                }
            }));
        }
        #endregion



     

        private void btn3_Click(object sender, RoutedEventArgs e)
        {
            WindowsIO.Initialize();
            if (btn3.Content.ToString() == "开始3")
            {
                btn3.Content = "停止";
                start = true;
                Task.Run(new Action(() =>
                {
                    while (true)
                    {
                        WindowsIO.KeyDownUp(66);
                        WindowsIO.KeyDownUp(65);
                        Thread.Sleep(2 * 1000);
                    }
                }));
            }
            else
            {
                btn3.IsEnabled = false;
                start = false;
                //while (true)
                //{
                //    if (t.Status == TaskStatus.RanToCompletion)
                //        break;
                //    Thread.Sleep(3000);
                //}

                Thread.Sleep(3000);

                btn3.Content = "开始3";
                btn3.IsEnabled = true;

            }


            
        }
    }

    public class WinIO
    {
        private const int KBC_KEY_CMD = 0x64;
        private const int KBC_KEY_DATA = 0x60;

        [DllImport("WinIo64.dll")]
        public static extern bool InitializeWinIo();

        [DllImport("WinIo64.dll")]
        public static extern bool GetPortVal(IntPtr wPortAddr, out int pdwPortVal, byte bSize);

        [DllImport("WinIo64.dll")]
        public static extern bool SetPortVal(uint wPortAddr, IntPtr dwPortVal, byte bSize);

        [DllImport("WinIo64.dll")]
        public static extern byte MapPhysToLin(byte pbPhysAddr, uint dwPhysSize, IntPtr PhysicalMemoryHandle);

        [DllImport("WinIo64.dll")]
        public static extern bool UnmapPhysicalMemory(IntPtr PhysicalMemoryHandle, byte pbLinAddr);

        [DllImport("WinIo64.dll")]
        public static extern bool GetPhysLong(IntPtr pbPhysAddr, byte pdwPhysVal);

        [DllImport("WinIo64.dll")]
        public static extern bool SetPhysLong(IntPtr pbPhysAddr, byte dwPhysVal);

        [DllImport("WinIo64.dll")]
        public static extern void ShutdownWinIo();

        [DllImport("user32.dll")]
        public static extern int MapVirtualKey(uint Ucode, uint uMapType);


        private WinIO()
        {
            IsInitialize = true;
        }
        public static void Initialize()
        {
            if (InitializeWinIo())
            {
                KBCWait4IBE();
                IsInitialize = true;
            }
            else
                System.Windows.MessageBox.Show("Load WinIO Failed!");
        }
        public static void Shutdown()
        {
            if (IsInitialize)
                ShutdownWinIo();
            IsInitialize = false;
        }

        private static bool IsInitialize { get; set; }

        ///等待键盘缓冲区为空
        private static void KBCWait4IBE()
        {
            int dwVal = 0;
            do
            {
                bool flag = GetPortVal((IntPtr)0x64, out dwVal, 1);
            }
            while ((dwVal & 0x2) > 0);
        }
        /// 模拟键盘标按下
        public static void KeyDown(Keys vKeyCoad)
        {
            if (!IsInitialize) return;

            int btScancode = 0;
            btScancode = MapVirtualKey((uint)vKeyCoad, 0);
            KBCWait4IBE();
            SetPortVal(KBC_KEY_CMD, (IntPtr)0xD2, 1);
            KBCWait4IBE();
            SetPortVal(KBC_KEY_DATA, (IntPtr)0x60, 1);
            KBCWait4IBE();
            SetPortVal(KBC_KEY_CMD, (IntPtr)0xD2, 1);
            KBCWait4IBE();
            SetPortVal(KBC_KEY_DATA, (IntPtr)btScancode, 1);
        }
        /// 模拟键盘弹出
        public static void KeyUp(Keys vKeyCoad)
        {
            if (!IsInitialize) return;

            int btScancode = 0;
            btScancode = MapVirtualKey((uint)vKeyCoad, 0);
            KBCWait4IBE();
            SetPortVal(KBC_KEY_CMD, (IntPtr)0xD2, 1);
            KBCWait4IBE();
            SetPortVal(KBC_KEY_DATA, (IntPtr)0x60, 1);
            KBCWait4IBE();
            SetPortVal(KBC_KEY_CMD, (IntPtr)0xD2, 1);
            KBCWait4IBE();
            SetPortVal(KBC_KEY_DATA, (IntPtr)(btScancode | 0x80), 1);
        }


        public static void KeyDownUp(Keys vKeyCoad)
        {
            Initialize(); // 注册
            KeyDown(vKeyCoad);
            System.Threading.Thread.Sleep(100);
            KeyUp(vKeyCoad);
            Shutdown(); // 用完后注销
        }
    }


    public class WindowsIO
    {
        public const int KBC_KEY_CMD = 0x64;
        public const int KBC_KEY_DATA = 0x60;

        private const int VK_NUMLOCK = 0x90; //数字锁定键
        private const int VK_SCROLL = 0x91; //滚动锁定
        private const int VK_CAPITAL = 0x14; //大小写锁定
        private const int VK_A = 65;
        private const int VK_TAB = 9;
        private const int VK_Delete = 46;
        private const int VK_ENTER = 13;
        private const int VK_END = 0x23;
        private const int VK_BACK = 0x08;
        private const int VK_SHIFT = 0x10;
        private const int VK_RETURN = 0x0D;
        private const int VK_ESCAPE = 0x1B;
        [DllImport("WinIo64.dll")]
        public static extern bool InitializeWinIo();
        [DllImport("WinIo64.dll")]
        public static extern bool GetPortVal(IntPtr wPortAddr, out int pdwPortVal, byte bSize);
        [DllImport("WinIo64.dll")]
        public static extern bool SetPortVal(uint wPortAddr, IntPtr dwPortVal, byte bSize);
        [DllImport("WinIo64.dll")]
        public static extern byte MapPhysToLin(byte pbPhysAddr, uint dwPhysSize, IntPtr PhysicalMemoryHandle);
        [DllImport("WinIo64.dll")]
        public static extern bool UnmapPhysicalMemory(IntPtr PhysicalMemoryHandle, byte pbLinAddr);
        [DllImport("WinIo64.dll")]
        public static extern bool GetPhysLong(IntPtr pbPhysAddr, byte pdwPhysVal);
        [DllImport("WinIo64.dll")]
        public static extern bool SetPhysLong(IntPtr pbPhysAddr, byte dwPhysVal);
        [DllImport("WinIo64.dll")]
        public static extern void ShutdownWinIo();

        [DllImport("user32.dll")]
        public static extern int MapVirtualKey(uint Ucode, uint uMapType);


        public static void Initialize()
        {
            if (InitializeWinIo())
            {
                KBCWait4IBE();
                IsInitialize = true;
            }
            else
                System.Windows.MessageBox.Show("Load WinIO Failed!");
        }
        public static void Shutdown()
        {
            if (IsInitialize)
                ShutdownWinIo();
            IsInitialize = false;
        }

        public static bool IsInitialize { get; set; }

        ///等待键盘缓冲区为空
        private static void KBCWait4IBE()
        {
            int dwVal = 0;
            do
            {
                bool flag = GetPortVal((IntPtr)0x64, out dwVal, 1);
            }
            while ((dwVal & 0x2) > 0);
        }
        /// 模拟键盘按下
        public static void MyKeyDown(int vKeyCoad)
        {
            if (!IsInitialize) return;

            int btScancode = 0;
            btScancode = MapVirtualKey((byte)vKeyCoad, 0);
            KBCWait4IBE();
            SetPortVal(KBC_KEY_CMD, (IntPtr)0xD2, 1);// 发送命令
            KBCWait4IBE();
            //SetPortVal(KBC_KEY_DATA, (IntPtr)0x60, 1);// 写入按键信息----这里四条被我注释掉了，因为网路上的其他人都没有注释掉，我查了资料，亲测，去掉这四条才能用。
            //KBCWait4IBE();
            //SetPortVal(KBC_KEY_CMD, (IntPtr)0xD2, 1);// 发送键盘写入命令
            //KBCWait4IBE();
            SetPortVal(KBC_KEY_DATA, (IntPtr)btScancode, 1);// 写入按下键
        }
        /// 模拟键盘弹出
        public static void MyKeyUp(int vKeyCoad)
        {
            if (!IsInitialize) return;

            int btScancode = 0;
            btScancode = MapVirtualKey((byte)vKeyCoad, 0);
            KBCWait4IBE();
            SetPortVal(KBC_KEY_CMD, (IntPtr)0xD2, 1);// 发送命令
            KBCWait4IBE();
            //SetPortVal(KBC_KEY_DATA, (IntPtr)0x60, 1);// 写入按键信息
            //KBCWait4IBE();
            //SetPortVal(KBC_KEY_CMD, (IntPtr)0xD2, 1);// 发送键盘写入命令
            //KBCWait4IBE();
            SetPortVal(KBC_KEY_DATA, (IntPtr)(btScancode | 0x80), 1);// 写入按下键
        }


        public static void KeyDownUp(int key)
        {
            //Initialize();
            MyKeyDown(key); // 按下A
            Thread.Sleep(200);
            MyKeyUp(key); // 松开A
            Shutdown();
        }
    }
}
