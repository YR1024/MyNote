using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DesktopIconTool.Helper
{

    public class TaskbarStyle
    {
        public enum AccentState
        {
            ACCENT_DISABLED = 0,                // 禁用任何背景或透明效果
            ACCENT_ENABLE_GRADIENT = 1,         // 启用渐变背景，但不透明
            ACCENT_ENABLE_TRANSPARENTGRADIENT = 2, // 启用渐变背景，并带有透明效果
            ACCENT_ENABLE_BLURBEHIND = 3,       // 启用模糊效果背后的透明效果
            ACCENT_ENABLE_ACRYLICBLURBEHIND = 4, // 启用亚克力样式的模糊透明效果，通常用于窗口和任务栏
            ACCENT_ENABLE_HOSTBACKDROP = 5,     // 启用托管背景的透明效果，应用于当前活动窗口后的背景
            ACCENT_INVALID_STATE = 6            // 无效状态，通常用于错误处理或状态未定义
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct AccentPolicy
        {
            public AccentState AccentState;
            public int AccentFlags;
            public int GradientColor;
            public int AnimationId;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct WindowCompositionAttributeData
        {
            public int Attribute;
            public IntPtr Data;
            public int SizeOfData;
        }

        public static void SetTaskbarTransparency(AccentState accentState)
        {
            foreach (var taskbarHwnd in getAllTaskbarHandles())
            {
                var accent = new AccentPolicy();
                accent.AccentState = accentState;
                var accentStructSize = Marshal.SizeOf(accent);
                var accentPtr = Marshal.AllocHGlobal(accentStructSize);
                Marshal.StructureToPtr(accent, accentPtr, false);

                var data = new WindowCompositionAttributeData();
                data.Attribute = 19;
                data.SizeOfData = accentStructSize;
                data.Data = accentPtr;

                SetWindowCompositionAttribute(taskbarHwnd, ref data);

                Marshal.FreeHGlobal(accentPtr);
            } 
            
        }

        static List<IntPtr> getAllTaskbarHandles()
        {
            // 主任务栏的类名
            string primaryTaskbarClassName = "Shell_TrayWnd";
            // 扩展任务栏的类名
            string secondaryTaskbarClassName = "Shell_SecondaryTrayWnd";

            // 获取主任务栏的句柄
            IntPtr primaryTaskbarHandle = FindWindowEx(IntPtr.Zero, IntPtr.Zero, primaryTaskbarClassName, null);

            // 获取扩展任务栏的句柄
            List<IntPtr> taskbarHandles = new List<IntPtr>();
            if (primaryTaskbarHandle != IntPtr.Zero)
            {
                taskbarHandles.Add(primaryTaskbarHandle);
            }

            // 查找所有扩展任务栏的句柄
            IntPtr secondaryTaskbarHandle = IntPtr.Zero;
            do
            {
                secondaryTaskbarHandle = FindWindowEx(IntPtr.Zero, secondaryTaskbarHandle, secondaryTaskbarClassName, null);
                if (secondaryTaskbarHandle != IntPtr.Zero)
                {
                    taskbarHandles.Add(secondaryTaskbarHandle);
                }
            } while (secondaryTaskbarHandle != IntPtr.Zero);

            return taskbarHandles;
        }



        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr FindWindowEx(IntPtr parentHandle, IntPtr childAfter, string className, string windowTitle);

        [DllImport("user32.dll")]
        public static extern int SetWindowCompositionAttribute(IntPtr hwnd, ref WindowCompositionAttributeData data);
    }
}


