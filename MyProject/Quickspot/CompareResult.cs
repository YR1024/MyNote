﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;

namespace Quickspot
{
    public class CompareResult
    {

        System.Windows.Window window;
        Grid windowRoot;

        private List<ImageInfo> _CompareInfo = new List<ImageInfo>();

        public CompareResult(List<ImageInfo> compareInfo)
        {
            InitWinodw();
            _CompareInfo = compareInfo;
            DrawDifferences();
        }

        private void InitWinodw()
        {
            window = new System.Windows.Window();
            window.SourceInitialized += delegate // 鼠标穿透
            {
                IntPtr hwnd = new WindowInteropHelper(window).Handle;
                uint extendedStyle = GetWindowLong(hwnd, GWL_EXSTYLE);
                SetWindowLong(hwnd, GWL_EXSTYLE, extendedStyle |
                WS_EX_TRANSPARENT);
            };
            //window.Title = "123";
            window.AllowsTransparency = true;
            window.WindowStyle = System.Windows.WindowStyle.None;
            window.ShowInTaskbar = false;
            window.WindowStartupLocation = System.Windows.WindowStartupLocation.CenterScreen;
            window.Width = 1024;
            window.Height = 768;
            window.Background = new SolidColorBrush(Colors.Transparent);
            window.Topmost = true;

            windowRoot = new Grid();
            window.Content = windowRoot;

            Border border = new Border();
            border.BorderBrush = new SolidColorBrush(Colors.Green);
            border.BorderThickness = new System.Windows.Thickness(1);
            windowRoot.Children.Add(border);
        }



        public void DrawDifferences()
        {
            foreach (var item in _CompareInfo)
            {
                if (item.Similarity > 0.9985)
                    continue;
                Color c = Colors.Yellow;
                double t = 0.5;
                if (item.Similarity < 0.95)
                {
                    c = Colors.Red;
                    t = 1;
                }
                //else if (item.Similarity >= 0.80 && item.Similarity < 0.90)
                //{
                //    c = Color.FromArgb(180,255,0,0);
                //    t = 0.8;
                //}
                //else if (item.Similarity >= 0.90 && item.Similarity < 0.95)
                //{
                //    c = Color.FromArgb(255, 255, 255, 0);
                //    t = 0.6;
                //}
                //else if (item.Similarity >= 0.95)
                //{
                //    c = Color.FromArgb(180, 255, 255, 0);
                //    t = 0.4;
                //}
                Border border = new Border();
                border.BorderThickness = new System.Windows.Thickness(t);
                border.BorderBrush = new SolidColorBrush(c);
                border.Width = ImageParse.splitBlockSize;
                border.Height = ImageParse.splitBlockSize;
                border.HorizontalAlignment = System.Windows.HorizontalAlignment.Left;
                border.VerticalAlignment = System.Windows.VerticalAlignment.Top;
                border.Margin = new System.Windows.Thickness(item.X, item.Y, 0, 0);

                windowRoot.Children.Add(border);
            }
        }

        public void ShowCompareResult()
        {
            window.Show();
        }

        public void CloseWindow()
        {
            window.Close();
        }

        private const int WS_EX_TRANSPARENT = 0x20;
        private const int GWL_EXSTYLE = (-20);
        [DllImport("user32", EntryPoint = "SetWindowLong")]
        private static extern uint SetWindowLong(IntPtr hwnd, int nIndex, uint dwNewLong);
        [DllImport("user32", EntryPoint = "GetWindowLong")]
        private static extern uint GetWindowLong(IntPtr hwnd, int nIndex);
    }
}
