using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace R_Auto_Task
{
    public enum DoAction
    {
        LeftMouseClick,
        LeftMouseDoubleClick,
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
        /// 波浪号 ,keys.192
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
        Up,
        Left,
        Down,
        Right,

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


    public class DoActionToKeysMapping
    {

        public static Keys GetKeys(DoAction action)
        {
            switch (action)
            {
                //case DoAction.LeftMouseClick :return Keys.LButton;
                //case DoAction.LeftMouseDoubleClick  :return Keys.LButton;
                //case DoAction.LeftMouseDown :return Keys.LButton;
                //case DoAction.LeftMouseUp :return Keys.LButton;
                //case DoAction.RightMouseClick :return Keys.;
                //case DoAction.RightMouseDoubleClick :return Keys.;
                //case DoAction.RightMouseDown :return Keys.;
                //case DoAction.RightMouseUp :return Keys.;
                //case DoAction.MiddleMouseClick :return Keys.;
                //case DoAction.MouseDownWheel :return Keys.;
                //case DoAction.MouseUpWheel :return Keys.;

                case DoAction.Esc: return Keys.Escape;
                case DoAction.F1: return Keys.F1;
                case DoAction.F2: return Keys.F2;
                case DoAction.F3: return Keys.F3;
                case DoAction.F4: return Keys.F4;
                case DoAction.F5: return Keys.F5;
                case DoAction.F6: return Keys.F6;
                case DoAction.F7: return Keys.F7;
                case DoAction.F8: return Keys.F8;
                case DoAction.F9: return Keys.F9;
                case DoAction.F10: return Keys.F10;
                case DoAction.F11: return Keys.F11;
                case DoAction.F12: return Keys.F12;
                case DoAction.Wave: return Keys.Oemtilde;
                case DoAction.Num1: return Keys.D1;
                case DoAction.Num2: return Keys.D2;
                case DoAction.Num3: return Keys.D3;
                case DoAction.Num4: return Keys.D4;
                case DoAction.Num5: return Keys.D5;
                case DoAction.Num6: return Keys.D6;
                case DoAction.Num7: return Keys.D7;
                case DoAction.Num8: return Keys.D8;
                case DoAction.Num9: return Keys.D9;
                case DoAction.Num0: return Keys.D0;
                case DoAction.Minus: return Keys.OemMinus;
                case DoAction.Plus: return Keys.Oemplus;
                case DoAction.Backspance: return Keys.Back;
                case DoAction.Tab: return Keys.Tab;
                case DoAction.Q: return Keys.Q;
                case DoAction.W: return Keys.W;
                case DoAction.E: return Keys.E;
                case DoAction.R: return Keys.R;
                case DoAction.T: return Keys.T;
                case DoAction.Y: return Keys.Y;
                case DoAction.U: return Keys.U;
                case DoAction.I: return Keys.I;
                case DoAction.O: return Keys.O;
                case DoAction.P: return Keys.P;
                case DoAction.LeftBrackets: return Keys.OemOpenBrackets;
                case DoAction.RightBrackets: return Keys.OemCloseBrackets;
                case DoAction.Pipe: return Keys.OemPipe;
                case DoAction.CapsLock: return Keys.CapsLock;
                case DoAction.A: return Keys.A;
                case DoAction.S: return Keys.S;
                case DoAction.D: return Keys.D;
                case DoAction.F: return Keys.F;
                case DoAction.G: return Keys.G;
                case DoAction.H: return Keys.H;
                case DoAction.J: return Keys.J;
                case DoAction.K: return Keys.K;
                case DoAction.L: return Keys.L;
                case DoAction.Semicolon: return Keys.OemSemicolon;
                case DoAction.Quotes: return Keys.OemQuotes;
                case DoAction.Enter: return Keys.Enter;
                case DoAction.LeftShift: return Keys.LShiftKey;
                case DoAction.Z: return Keys.Z;
                case DoAction.X: return Keys.X;
                case DoAction.C: return Keys.C;
                case DoAction.V: return Keys.V;
                case DoAction.B: return Keys.B;
                case DoAction.N: return Keys.N;
                case DoAction.M: return Keys.M;
                case DoAction.Comma: return Keys.Oemcomma;
                case DoAction.Period: return Keys.OemPeriod;
                case DoAction.Question: return Keys.OemQuestion;
                case DoAction.RightShift: return Keys.RShiftKey;
                case DoAction.LeftCtrl: return Keys.LControlKey;
                case DoAction.LeftWin: return Keys.LWin;
                case DoAction.LeftAlt: return Keys.LMenu;
                case DoAction.Space: return Keys.Space;
                case DoAction.RightAlt: return Keys.RMenu;
                case DoAction.RightWin: return Keys.RWin;
                case DoAction.Apps: return Keys.Apps;
                case DoAction.RightCtrl: return Keys.RControlKey;
                case DoAction.PrtSc: return Keys.PrintScreen;
                case DoAction.ScrollLock: return Keys.Scroll;
                case DoAction.PauseBreak: return Keys.Pause;
                case DoAction.Insert: return Keys.Insert;
                case DoAction.Home: return Keys.Home;
                case DoAction.PageUp: return Keys.PageUp;
                case DoAction.Delete: return Keys.Delete;
                case DoAction.End: return Keys.End;
                case DoAction.PageDown: return Keys.PageDown;
                case DoAction.Up: return Keys.Up;
                case DoAction.Left: return Keys.Left;
                case DoAction.Down: return Keys.Down;
                case DoAction.Right: return Keys.Right;

                case DoAction.NumLock: return Keys.NumLock;
                case DoAction.Divide: return Keys.Divide;
                case DoAction.Multiply: return Keys.Multiply;
                case DoAction.Subtract: return Keys.Subtract;
                case DoAction.Add: return Keys.Add;
                case DoAction.NumPad0: return Keys.NumPad0;
                case DoAction.NumPad1: return Keys.NumPad1;
                case DoAction.NumPad2: return Keys.NumPad2;
                case DoAction.NumPad3: return Keys.NumPad3;
                case DoAction.NumPad4: return Keys.NumPad4;
                case DoAction.NumPad5: return Keys.NumPad5;
                case DoAction.NumPad6: return Keys.NumPad6;
                case DoAction.NumPad7: return Keys.NumPad7;
                case DoAction.NumPad8: return Keys.NumPad8;
                case DoAction.NumPad9: return Keys.NumPad9;
                case DoAction.Decimal: return Keys.Decimal;
                default: return Keys.Modifiers;
            }
        }
    }
}
