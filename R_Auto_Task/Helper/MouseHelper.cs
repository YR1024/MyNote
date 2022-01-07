using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace R_Auto_Task.Helper
{
    class MouseHelper
    {
        [System.Runtime.InteropServices.DllImport("user32")]
        private static extern int mouse_event(int dwFlags, int dx, int dy, int dwData, int dwExtraInfo);
        //移动鼠标 
        const int MOUSEEVENTF_MOVE = 0x0001;
        //模拟鼠标左键按下 
        const int MOUSEEVENTF_LEFTDOWN = 0x0002;
        //模拟鼠标左键抬起 
        const int MOUSEEVENTF_LEFTUP = 0x0004;
        //模拟鼠标右键按下 
        const int MOUSEEVENTF_RIGHTDOWN = 0x0008;
        //模拟鼠标右键抬起 
        const int MOUSEEVENTF_RIGHTUP = 0x0010;
        //模拟鼠标中键按下 
        const int MOUSEEVENTF_MIDDLEDOWN = 0x0020;
        //模拟鼠标中键抬起 
        const int MOUSEEVENTF_MIDDLEUP = 0x0040;
        //标示是否采用绝对坐标 
        const int MOUSEEVENTF_ABSOLUTE = 0x8000;
        //模拟鼠标滚轮滚动操作，必须配合dwData参数
        const int MOUSEEVENTF_WHEEL = 0x0800;


        public static void TestMoveMouse()
        {
            Console.WriteLine("模拟鼠标移动5个像素点。");
            //mouse_event(MOUSEEVENTF_MOVE, 50, 50, 0, 0);//相对当前鼠标位置x轴和y轴分别移动50像素
            mouse_event(MOUSEEVENTF_WHEEL, 0, 0, -20, 0);//鼠标滚动，使界面向下滚动20的高度
        }

        public static void MouseDownUp(int X, int Y)
        {
            Console.WriteLine("模拟鼠标移动5个像素点。");
            mouse_event(MOUSEEVENTF_ABSOLUTE | MOUSEEVENTF_MOVE, X * 65536 / 1920, Y * 65536 / 1080, 0, 0);
            mouse_event(MOUSEEVENTF_LEFTDOWN | MOUSEEVENTF_LEFTUP, 0, 0, 0, 0);
            //mouse_event(MOUSEEVENTF_LEFTDOWN, X * 65536 / 1920, Y * 65536 / 1080, 0, 0);
            //mouse_event(MOUSEEVENTF_LEFTUP, X * 65536 / 1920, Y * 65536 / 1080, 0, 0);
        }
    }
}
