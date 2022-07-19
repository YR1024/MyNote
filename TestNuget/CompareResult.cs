using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Media;

namespace TestNuget
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
                if (item.Similarity > 0.997)
                    continue;
                Color c = Colors.Yellow;
                if (item.Similarity < 0.95)
                {
                    c = Colors.Red;
                }
                Border border = new Border();
                border.BorderThickness = new System.Windows.Thickness(1);
                border.BorderBrush = new SolidColorBrush(c);
                border.Width = Quickspot.splitBlockSize;
                border.Height = Quickspot.splitBlockSize;
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
    }
}
