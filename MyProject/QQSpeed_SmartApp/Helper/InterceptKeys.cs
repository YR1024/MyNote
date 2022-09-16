using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace WpfApp
{
    /// <summary>
    /// 获取键盘按键
    /// </summary>
    public class InterceptKeys
    {
        private const int WH_KEYBOARD_LL = 13; //全局键盘钩子
        private const int WM_KEYDOWN = 0x0100; //键盘按下
        private const int WM_KEYUP = 0x0101; //键盘抬起
        private static LowLevelKeyboardProc _proc = HookCallback;
        private static IntPtr _hookID = IntPtr.Zero;

        #region 调用API

        private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

        /// <summary>
        /// 安装钩子
        /// </summary>
        /// <param name="idHook">钩子类型</param>
        /// <param name="lpfn">函数指针</param>
        /// <param name="hMod">包含钩子函数的模块(EXE、DLL)句柄; 一般是 HInstance; 如果是当前线程这里可以是 0</param>
        /// <param name="dwThreadId">关联的线程; 可用 GetCurrentThreadId 获取当前线程; 0 表示是系统级钩子</param>
        /// <returns>返回钩子的句柄; 0 表示失败</returns>
        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

        //钩子类型 idHook 选项:
        //WH_MSGFILTER       = -1; {线程级; 截获用户与控件交互的消息}
        //WH_JOURNALRECORD   = 0;  {系统级; 记录所有消息队列从消息队列送出的输入消息, 在消息从队列中清除时发生; 可用于宏记录}
        //WH_JOURNALPLAYBACK = 1;  {系统级; 回放由 WH_JOURNALRECORD 记录的消息, 也就是将这些消息重新送入消息队列}
        //WH_KEYBOARD        = 2;  {系统级或线程级; 截获键盘消息}
        //WH_GETMESSAGE      = 3;  {系统级或线程级; 截获从消息队列送出的消息}
        //WH_CALLWNDPROC     = 4;  {系统级或线程级; 截获发送到目标窗口的消息, 在 SendMessage 调用时发生}
        //WH_CBT             = 5;  {系统级或线程级; 截获系统基本消息, 譬如: 窗口的创建、激活、关闭、最大最小化、移动等等}
        //WH_SYSMSGFILTER    = 6;  {系统级; 截获系统范围内用户与控件交互的消息}
        //WH_MOUSE           = 7;  {系统级或线程级; 截获鼠标消息}
        //WH_HARDWARE        = 8;  {系统级或线程级; 截获非标准硬件(非鼠标、键盘)的消息}
        //WH_DEBUG           = 9;  {系统级或线程级; 在其他钩子调用前调用, 用于调试钩子}
        //WH_SHELL           = 10; {系统级或线程级; 截获发向外壳应用程序的消息}
        //WH_FOREGROUNDIDLE  = 11; {系统级或线程级; 在程序前台线程空闲时调用}
        //WH_CALLWNDPROCRET  = 12; {系统级或线程级; 截获目标窗口处理完毕的消息, 在 SendMessage 调用后发生}
        //WH_KEYBOARD_LL     = 13; {系统级; 截获低级键盘消息}
        //WH_MOUSE_LL        = 14; {系统级; 截获低级鼠标消息}

        /// <summary>
        /// 卸载钩子
        /// </summary>
        /// <param name="hhk">钩子的句柄</param>
        /// <returns></returns>
        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr GetModuleHandle(string lpModuleName);

        #endregion

        /// <summary>
        /// 安装钩子
        /// </summary>
        public static void SetHook()
        {
            using (Process curProcess = Process.GetCurrentProcess())
            using (ProcessModule curModule = curProcess.MainModule)
            {
                _hookID = SetWindowsHookEx(WH_KEYBOARD_LL, _proc, GetModuleHandle(curModule.ModuleName), 0);
            }
        }

        /// <summary>
        /// 处理函数
        /// </summary>
        /// <param name="nCode"></param>
        /// <param name="wParam"></param>
        /// <param name="lParam"></param>
        /// <returns>
        /// 如果返回1，则结束消息，这个消息到此为止，不再传递；
        /// 如果返回0或调用CallNextHookEx函数则消息出了这个钩子继续往下传递，也就是传给消息真正的接受者；
        /// </returns>
        private static IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            ////键盘按下时
            //if (nCode >= 0 && wParam == (IntPtr)WM_KEYDOWN)
            //{
            //    int vkCode = Marshal.ReadInt32(lParam);
            //    Keys key = (Keys)vkCode;

            //    //记录到日志
            //    //File.AppendAllText(@"C:\hot.txt", DateTime.Now.ToString("HH:mm:ss") + ": " + key.ToString() + "\r\n");
            //    Console.WriteLine(key.ToString());
            //}


            //int vkCode = Marshal.ReadInt32(lParam);
            //Keys key = (Keys)vkCode;
            KeyInfo.Invoke((Keys)Marshal.ReadInt32(lParam), wParam.ToInt32());
            return CallNextHookEx(_hookID, nCode, wParam, lParam);
        }

        /// <summary>
        /// 卸载钩子
        /// </summary>
        public static void UnHook()
        {
            if (_hookID != IntPtr.Zero)
            {
                UnhookWindowsHookEx(_hookID);
            }
        }


        public static Action<Keys, int> KeyInfo = delegate { };
    }

}
