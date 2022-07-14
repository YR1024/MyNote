using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace R_Auto_Task.Helper
{
    public class ScreenshotManager
    {
        private static ScreenshotManager _instance;
        public static ScreenshotManager Instance
        {
            get {
                if (_instance == null)
                {
                    _instance = new ScreenshotManager();
                }
                return _instance;
            }
            private set { _instance = value; }
        }

        bool IsMouseDown { get; set; } = false;

        System.Windows.Point MouseDownPoint { get; set; }

        System.Windows.Shapes.Rectangle RectCtrl { get; set; }

        Window ImageWnd { get; set; }

        System.Drawing.Image fullScreenImage { get; set; }

        System.Drawing.Image TargetImage { get; set; }

        public System.Drawing.Image Screenshot()
        {
            ImageWnd = new Window();
            ImageWnd.MouseLeftButtonDown += ImageWnd_MouseLeftButtonDown; ;
            ImageWnd.MouseMove += ImageWnd_MouseMove;
            ImageWnd.MouseLeftButtonUp += ImageWnd_MouseLeftButtonUp;
            ImageWnd.KeyDown += ImageWnd_KeyDown; ;

            ImageWnd.WindowStyle = WindowStyle.None;
            ImageWnd.ShowInTaskbar = false;
            ImageWnd.WindowState = WindowState.Maximized;
            fullScreenImage = Pranas.ScreenshotCapture.TakeScreenshot(true);
            System.Windows.Controls.Image ImgCtrl = new System.Windows.Controls.Image();
            ImgCtrl.Source = ImageHelper.BitmapToImageSource(new Bitmap(fullScreenImage));

            Grid grid = new Grid();
            grid.Children.Add(ImgCtrl);

            ImageWnd.Content = grid;

            if(ImageWnd.ShowDialog() == true)
            {
                return TargetImage;
            }
            else
            {
                return null;
            }

        }

        /// <summary>
        /// ESC键 退出
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ImageWnd_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                ImageWnd.Close();
            }
        }
        
        /// <summary>
        /// 左键按下
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ImageWnd_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            IsMouseDown = true;
            Grid grid = (sender as Window).Content as Grid;
            MouseDownPoint = e.GetPosition(grid);

            if (grid.Children.Count > 1)
            {
                if (grid.Children.Count == 4)
                    grid.Children.RemoveAt(3);
                return;
            }
            Border border = new Border()
            {
                Background = new SolidColorBrush(Colors.Black),
                Opacity = 0.5,
            };
            grid.Children.Add(border);

            RectCtrl = new System.Windows.Shapes.Rectangle()
            {
                Stroke = new SolidColorBrush(Colors.Green),
                StrokeThickness = 1,
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Top,
                Margin = new Thickness(MouseDownPoint.X, MouseDownPoint.Y, 0, 0),
                Height = 0,
                Width = 0,
            };
            grid.Children.Add(RectCtrl);
        }

        /// <summary>
        /// 左键弹起
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ImageWnd_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            IsMouseDown = false;

            Grid grid = (sender as Window).Content as Grid;

            StackPanel stackPanel = new StackPanel()
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Top,
                Margin = new Thickness(0, RectCtrl.Margin.Top + RectCtrl.Height, 1920 - RectCtrl.Width - RectCtrl.Margin.Left, 0),
                Height = 40,
            };
            if (RectCtrl.Margin.Top + RectCtrl.Height > 1040)
                stackPanel.Margin = new Thickness(0, RectCtrl.Margin.Top + RectCtrl.Height - 40, 1920 - RectCtrl.Width - RectCtrl.Margin.Left, 0);
            var OKBtn = new Button();
            OKBtn.Height = 30;
            OKBtn.Width = 50;
            OKBtn.HorizontalAlignment = HorizontalAlignment.Right;
            OKBtn.Content = "确定";
            OKBtn.Click += OKBtn_Click;

            var CancelBtn = new Button();
            CancelBtn.Height = 30;
            CancelBtn.Width = 50;
            CancelBtn.HorizontalAlignment = HorizontalAlignment.Right;
            CancelBtn.Content = "取消";
            CancelBtn.Click += CancelBtn_Click;
            stackPanel.Children.Add(CancelBtn);
            stackPanel.Children.Add(OKBtn);
            grid.Children.Add(stackPanel);

        }

        /// <summary>
        /// 左键移动
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ImageWnd_MouseMove(object sender, MouseEventArgs e)
        {
            if (!IsMouseDown)
                return;
            Grid grid = (sender as Window).Content as Grid;
            var _mousePoint = e.GetPosition(grid);
            if (grid.Children.Count < 2)
                return;
            var rect = grid.Children[2] as System.Windows.Shapes.Rectangle;

            var h = _mousePoint.Y - MouseDownPoint.Y;
            var w = _mousePoint.X - MouseDownPoint.X;

            if (h >= 0 && w >= 0)
            {
                rect.Margin = new Thickness(MouseDownPoint.X, MouseDownPoint.Y, 0, 0);
                rect.Height = h;
                rect.Width = w;
            }
            else if (h < 0 && w < 0)
            {
                rect.Margin = new Thickness(_mousePoint.X, _mousePoint.Y, 0, 0);
                rect.Height = Math.Abs(h);
                rect.Width = Math.Abs(w);
            }
            else if (h < 0 && w >= 0)
            {
                rect.Margin = new Thickness(MouseDownPoint.X, _mousePoint.Y, 0, 0);
                rect.Height = Math.Abs(h);
                rect.Width = Math.Abs(w);
            }
            else if (h >= 0 && w < 0)
            {
                rect.Margin = new Thickness(_mousePoint.X, MouseDownPoint.Y, 0, 0);
                rect.Height = Math.Abs(h);
                rect.Width = Math.Abs(w);
            }

            (grid.Children[1] as Border).Clip = new PathGeometry()
            {
                Figures = new PathFigureCollection()
                {
                    new PathFigure()
                    {
                        StartPoint = new System.Windows.Point(0,0),
                        Segments = new PathSegmentCollection()
                        {
                             new LineSegment(){Point=new System.Windows.Point(1920,0)},
                             new LineSegment(){Point=new System.Windows.Point(1920,1080)},
                             new LineSegment(){Point=new System.Windows.Point(0,1080)}
                        }
                    },
                    new PathFigure()
                    {
                        StartPoint = new System.Windows.Point(rect.Margin.Left, rect.Margin.Top),
                        Segments = new PathSegmentCollection()
                        {
                             new LineSegment(){Point=new System.Windows.Point(rect.Margin.Left + rect.Width, rect.Margin.Top)},
                             new LineSegment(){Point=new System.Windows.Point(rect.Margin.Left + rect.Width, rect.Margin.Top + rect.Height)},
                             new LineSegment(){Point=new System.Windows.Point(rect.Margin.Left, rect.Margin.Top + rect.Height)}
                        }
                    },
                }
            };
        }

        /// <summary>
        /// 确定
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OKBtn_Click(object sender, RoutedEventArgs e)
        {
            TargetImage = CaptureImage(fullScreenImage, (int)RectCtrl.Margin.Left, (int)RectCtrl.Margin.Top, (int)RectCtrl.Width + 1, (int)RectCtrl.Height + 1);
            ImageWnd.DialogResult = true;
        }

        /// <summary>
        /// 取消
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CancelBtn_Click(object sender, RoutedEventArgs e)
        {
            ImageWnd.DialogResult = true;
        }

        /// <summary>
        /// 从大图中截取一部分图片
        /// </summary>
        /// <param name="fromImagePath">来源图片地址</param>        
        /// <param name="offsetX">从偏移X坐标位置开始截取</param>
        /// <param name="offsetY">从偏移Y坐标位置开始截取</param>
        /// <param name="width">保存图片的宽度</param>
        /// <param name="height">保存图片的高度</param>
        /// <returns></returns>
        public System.Drawing.Image CaptureImage(System.Drawing.Image fromImage, int offsetX, int offsetY, int width, int height)
        {
            //创建新图位图
            Bitmap bitmap = new Bitmap(width, height);
            //创建作图区域
            Graphics graphic = Graphics.FromImage(bitmap);
            //截取原图相应区域写入作图区
            graphic.DrawImage(fromImage, 0, 0, new Rectangle(offsetX, offsetY, width, height), GraphicsUnit.Pixel);
            //从作图区生成新图
            System.Drawing.Image saveImage = System.Drawing.Image.FromHbitmap(bitmap.GetHbitmap());
            //保存图片
            //saveImage.Save(toImagePath, ImageFormat.Png);
            //释放资源   
            graphic.Dispose();
            bitmap.Dispose();
            //saveImage.Dispose();
            return saveImage;
        }

    }
}
