using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace R_Auto_Task.Helper
{
    class WinIoHelper
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


        private WinIoHelper()
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
}
