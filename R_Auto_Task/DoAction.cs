using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace R_Auto_Task
{
    public enum DoAction
    {
        LeftMouseClick,
        LeftMouseDoubleClick ,
        LeftMouseDown,
        LeftMouseUp,

        RightMouseClick,
        RightMouseDoubleClick,
        RightMouseDown,
        RightMouseUp,

        MiddleMouseClick,

        MouseDownWheel,
        MouseUpWheel,


        Esc,
        F1,
        F2,
        F3,
        F4,
        F5,
        F6,
        F7,
        F8,
        F9,
        F10,
        F11,
        F12,

        /// <summary>
        /// 波浪号
        /// </summary>
        Wave, 
        Num1,
        Num2,
        Num3,
        Num4,
        Num5,
        Num6,
        Num7,
        Num8,
        Num9,
        Num0,
        /// <summary>
        /// 减
        /// </summary>
        Minus,
        /// <summary>
        /// 加
        /// </summary>
        Plus,
        /// <summary>
        /// 回格
        /// </summary>
        Backspance,

        Tab,
        Q,
        W,
        E,
        R,
        T,
        Y,
        U,
        I,
        O,
        P,
        /// <summary>
        /// 左括号
        /// </summary>
        LeftBrackets,
        /// <summary>
        /// 右括号
        /// </summary>
        RightBrackets,
        /// <summary>
        /// 管道键 |\
        /// </summary>
        Pipe,

        /// <summary>
        /// 大写锁定
        /// </summary>
        CapsLock,
        A,
        S,
        D,
        F,
        G,
        H,
        J,
        K,
        L,
        /// <summary>
        /// 分号
        /// </summary>
        Semicolon,
        /// <summary>
        /// 单/双引号
        /// </summary>
        Quotes,
        /// <summary>
        /// 回车
        /// </summary>
        Enter,

        LeftShift,
        Z,
        X,
        C,
        V,
        B,
        N,
        M,
        /// <summary>
        /// 逗号 
        /// </summary>
        Comma,
        /// <summary>
        /// 句号键
        /// </summary>
        Period,
        /// <summary>
        /// 问号键
        /// </summary>
        Question,
        RightShift,

        LeftCtrl,
        LeftWin,
        LeftAlt,
        Space,
        RightAlt,
        RightWin,
        /// <summary>
        /// 菜单键 ，应用程序键（Microsoft Natural Keyboard，人体工程学键盘）。
        /// </summary>
        Apps,
        RightCtrl,


        /// <summary>
        /// PrintScreen
        /// </summary>
        PrtSc,
        /// <summary>
        /// Scroll
        /// </summary>
        ScrollLock,
        /// <summary>
        /// Pause
        /// </summary>
        PauseBreak,
        Insert,
        Home,
        PageUp,
        Delete,
        End,
        PageDown,

        NumLock,
        /// <summary>
        /// 小键盘上 除号
        /// </summary>
        Divide,
        /// <summary>
        /// 小键盘上 乘号
        /// </summary>
        Multiply,
        /// <summary>
        /// 小键盘上 减号
        /// </summary>
        Subtract,
        /// <summary>
        /// 小键盘上 加号
        /// </summary>
        Add,

        NumPad0,
        NumPad1,
        NumPad2,
        NumPad3,
        NumPad4,
        NumPad5,
        NumPad6,
        NumPad7,
        NumPad8,
        NumPad9,

        /// <summary>
        /// 小键盘上 点 delete
        /// </summary>
        Decimal,

    }


}
