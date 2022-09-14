using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace AutomationServices.EmguCv.Helper
{
    public class KeyBoardHelper
    {
        public static void KeyDownUp(Keys key)
        {
            KeyDown(key);
            KeyUp(key);
        }

        public static void KeyDown(Keys key)
        {
            //Input[] myInput = new Input[1];
            //myInput[0].type = 1;//模拟键盘
            //myInput[0].ki.wVk = (short)key.GetHashCode();
            //myInput[0].ki.dwFlags = 0;//按下
            //SendInput(1u, myInput, Marshal.SizeOf((object)default(Input)));


            INPUT[] myInput = new INPUT[1];
            myInput[0].Type = 1;//模拟键盘
            myInput[0].U.ki.wVk = (short)key.GetHashCode();
            myInput[0].U.ki.dwFlags = 0;//按下
            SendInput(1u, myInput, Marshal.SizeOf((object)default(INPUT)));

        }

        public static void KeyUp(Keys key)
        {
            INPUT[] myInput = new INPUT[1];
            myInput[0].Type = 1;//模拟键盘
            myInput[0].U.ki.wVk = (short)key.GetHashCode();
            myInput[0].U.ki.dwFlags = 2;//抬起
            SendInput(1u, myInput, Marshal.SizeOf((object)default(INPUT)));
        }

        //[DllImport("user32")]
        //public static extern uint SendInput(uint nInputs, Input[] pInputs, int cbSize);

        [DllImport("user32")]
        public static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);


        const int MouseEvent_Absolute = 0x8000;
        const int MouserEvent_Hwheel = 0x01000;
        const int MouseEvent_Move = 0x0001;
        const int MouseEvent_Move_noCoalesce = 0x2000;
        const int MouseEvent_LeftDown = 0x0002;
        const int MouseEvent_LeftUp = 0x0004;
        const int MouseEvent_MiddleDown = 0x0020;
        const int MouseEvent_MiddleUp = 0x0040;
        const int MouseEvent_RightDown = 0x0008;
        const int MouseEvent_RightUp = 0x0010;
        const int MouseEvent_Wheel = 0x0800;
        const int MousseEvent_XUp = 0x0100;
        const int MousseEvent_XDown = 0x0080;

        const int VK_SHIFT = 0x10;	//16	Shift键
        const int VK_CONTROL = 0x11;	//17	Ctrl键
        const int VK_MENU = 0x12;   //18	Alt键

        //void AA()
        //{
        //    for (i = X; i <= X + width; i += 450)

        //    //X为Flash窗口的左上角的x轴绝对坐标值。屏幕左上角坐标是（0,0）。width是Flash窗口宽度。
        //    {

        //        for (j = Y; j <= Y + height; j += 150) //Y为Flash窗口的左上角的y轴绝对坐标值。height是Flash窗口高度。
        //        {

        //            MouseInput myMinput = new MouseInput();
        //            myMinput.dx = i;
        //            myMinput.dy = j;
        //            myMinput.Mousedata = 0;
        //            myMinput.dwFlag = MouseEvent_Absolute | MouseEvent_Move | MouseEvent_LeftDown | MouseEvent_LeftUp;

        //            myMinput.time = 0;
        //            Input[] myInput = new Input[1];
        //            myInput[0] = new Input();
        //            myInput[0].type = 0;
        //            myInput[0].mi = myMinput;

        //            UInt32 result = SendInput((uint)myInput.Length, myInput, Marshal.SizeOf(myInput[0].GetType()));
        //            if (result == 0)
        //            {
        //                MessageBox.Show("fail");
        //            }
        //        }
        //    }
        //}

        //public void SimulateInputString(string sText)
        //{
        //    char[] cText = sText.ToCharArray();
        //    foreach (char c in cText)
        //    {
        //        Input[] input = new Input[2];
        //        if (c >= 0 && c < 256)//a-z A-Z
        //        {
        //            short num = VkKeyScan(c);//获取虚拟键码值
        //            if (num != -1)
        //            {
        //                bool shift = (num >> 8 & 1) != 0;//num >>8表示 高位字节上当状态，如果为1则按下Shift，否则没有按下Shift，即大写键CapsLk没有开启时，是否需要按下Shift。
        //                if ((GetKeyState(20) & 1) != 0 && ((c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z')))//Win32API.GetKeyState(20)获取CapsLk大写键状态
        //                {
        //                    shift = !shift;
        //                }
        //                if (shift)
        //                {
        //                    input[0].type = 1;//模拟键盘
        //                    input[0].ki.wVk = 16;//Shift键
        //                    input[0].ki.dwFlags = 0;//按下
        //                    SendInput(1u, input, Marshal.SizeOf((object)default(Input)));
        //                }
        //                input[0].type = 1;
        //                input[0].ki.wVk = (short)(num & 0xFF);
        //                input[1].type = 1;
        //                input[1].ki.wVk = (short)(num & 0xFF);
        //                input[1].ki.dwFlags = 2;
        //                SendInput(2u, input, Marshal.SizeOf((object)default(Input)));
        //                if (shift)
        //                {
        //                    input[0].type = 1;
        //                    input[0].ki.wVk = 16;
        //                    input[0].ki.dwFlags = 2;//抬起
        //                    SendInput(1u, input, Marshal.SizeOf((object)default(Input)));
        //                }
        //                continue;
        //            }
        //        }
        //        input[0].type = 1;
        //        input[0].ki.wVk = 0;//dwFlags 为KEYEVENTF_UNICODE 即4时，wVk必须为0
        //        input[0].ki.wScan = (short)c;
        //        input[0].ki.dwFlags = 4;//输入UNICODE字符
        //        input[0].ki.time = 0;
        //        input[0].ki.dwExtraInfo = IntPtr.Zero;
        //        input[1].type = 1;
        //        input[1].ki.wVk = 0;
        //        input[1].ki.wScan = (short)c;
        //        input[1].ki.dwFlags = 6;
        //        input[1].ki.time = 0;
        //        input[1].ki.dwExtraInfo = IntPtr.Zero;
        //        SendInput(2u, input, Marshal.SizeOf((object)default(Input)));
        //    }
        //}

    }




    //[StructLayout(LayoutKind.Explicit)]
    //public struct Input
    //{
    //    [FieldOffset(0)]
    //    public Int32 type;

    //    [FieldOffset(0)]
    //    public MouseInput mi;

    //    [FieldOffset(0)]
    //    public KeyBdInput ki;

    //    [FieldOffset(0)]
    //    public HARDWAREINPUT hi;
    //}


    [StructLayout(LayoutKind.Explicit)]
    public struct INPUT
    {
        [FieldOffset(0)] public uint Type;
        [FieldOffset(8)] public InputUnion U;
    }
    [StructLayout(LayoutKind.Explicit)]
    public struct InputUnion
    {
        [FieldOffset(0)] 
        public MouseInput mi;

        [FieldOffset(0)]
        public KeyBdInput ki;

        [FieldOffset(0)]
        public HARDWAREINPUT hi;
    }
    


    [StructLayout(LayoutKind.Sequential)]
    public struct MouseInput
    {
        public Int32 dx;
        public Int32 dy;
        public Int32 Mousedata;
        public Int32 dwFlag;
        public Int32 time;
        public IntPtr dwExtraInfo;
    }
    [StructLayout(LayoutKind.Sequential)]
    public struct KeyBdInput
    {
        public Int16 wVk;
        public Int16 wScan;
        public Int32 dwFlags;
        public Int32 time;
        public IntPtr dwExtraInfo;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct HARDWAREINPUT
    {
        Int32 uMsg;
        Int16 wParamL;
        Int16 wParamH;
    }
}
