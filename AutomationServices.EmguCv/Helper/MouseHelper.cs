using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutomationServices.EmguCv.Helper
{
    public class MouseHelper
    {
        [System.Runtime.InteropServices.DllImport("user32")]
        private static extern int mouse_event(int dwFlags, int dx, int dy, int dwData, int dwExtraInfo);
        //移动鼠标 
        const int MouseEventf_Move = 0x0001;
        //模拟鼠标左键按下 
        const int MouseEventf_LeftDown = 0x0002;
        //模拟鼠标左键抬起 
        const int MouseEventf_LeftUp = 0x0004;
        //模拟鼠标右键按下 
        const int MouseEventf_RightDown = 0x0008;
        //模拟鼠标右键抬起 
        const int MouseEventf_RightUp = 0x0010;
        //模拟鼠标中键按下 
        const int MouseEventf_MiddleDown = 0x0020;
        //模拟鼠标中键抬起 
        const int MouseEventf_MiddleUp = 0x0040;
        //标示是否采用绝对坐标 
        const int MouseEventf_Absolute = 0x8000;
        //模拟鼠标滚轮滚动操作，必须配合dwData参数
        const int MouseEventf_Wheel = 0x0800;


        public static void TestMoveMouse()
        {
            Console.WriteLine("模拟鼠标移动5个像素点。");
            //mouse_event(MOUSEEVENTF_MOVE, 50, 50, 0, 0);//相对当前鼠标位置x轴和y轴分别移动50像素
            mouse_event(MouseEventf_Wheel, 0, 0, -20, 0);//鼠标滚动，使界面向下滚动20的高度
        }

        public static void MouseDownWheel(int Value)
        {
            mouse_event(MouseEventf_Wheel, 0, 0, -Value, 0);
        }

        public static void MouseUpWheel(int Value)
        {
            mouse_event(MouseEventf_Wheel, 0, 0, Value, 0);
        }

        public static void MouseMove(int X, int Y)
        {
            mouse_event(MouseEventf_Absolute | MouseEventf_Move, X * 65536 / 1920, Y * 65536 / 1080, 0, 0);
        }

        public static void MouseDownUp()
        {
            mouse_event(MouseEventf_LeftDown | MouseEventf_LeftUp, 0, 0, 0, 0);
        }
        public static void MouseDown()
        {
            mouse_event(MouseEventf_LeftDown, 0, 0, 0, 0);
        }
        public static void MouseUp()
        {
            mouse_event(MouseEventf_LeftUp, 0, 0, 0, 0);
        }
        public static void MouseDownUp(int X, int Y)
        {
            MouseMove(X, Y);
            MouseDownUp();
            //mouse_event(MOUSEEVENTF_LEFTDOWN, X * 65536 / 1920, Y * 65536 / 1080, 0, 0);
            //mouse_event(MOUSEEVENTF_LEFTUP, X * 65536 / 1920, Y * 65536 / 1080, 0, 0);
        }
        public static void MouseDown(int X, int Y)
        {
            MouseMove(X, Y);
            MouseDown();
        }
        public static void MouseUp(int X, int Y)
        {
            MouseMove(X, Y);
            MouseUp();
        }

        public static void RightMouseDownUp()
        {
            mouse_event(MouseEventf_RightDown | MouseEventf_RightUp, 0, 0, 0, 0);
        }
        public static void RightMouseDown()
        {
            mouse_event(MouseEventf_RightDown, 0, 0, 0, 0);
        }
        public static void RightMouseUp()
        {
            mouse_event(MouseEventf_RightUp, 0, 0, 0, 0);
        }
        public static void RightMouseDownUp(int X, int Y)
        {
            MouseMove(X, Y);
            RightMouseDownUp();
        }
        public static void RightMouseDown(int X, int Y)
        {
            MouseMove(X, Y);
            RightMouseDown();
        }
        public static void RightMouseUp(int X, int Y)
        {
            MouseMove(X, Y);
            RightMouseUp();
        }
    }
}
