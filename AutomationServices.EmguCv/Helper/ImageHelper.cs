using SharpDX;
using SharpDX.Direct3D12;
using SharpDX.DXGI;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Media.Imaging;

namespace AutomationServices.EmguCv.Helper
{
    public class ImageHelper
    {
        /// <summary>
        /// 将bitmap转换成base64字符串 
        /// </summary>
        /// <param name="bitmap">bitmap</param>
        /// <returns>base64字符串 </returns>
        public static string BitmapToString(Bitmap bitmap)
        {
            using (var memoryStream = new MemoryStream())
            {
                bitmap.Save(memoryStream, ImageFormat.Jpeg);
                byte[] byteImage = memoryStream.ToArray();
                // Get Base64
                return Convert.ToBase64String(byteImage);
            }
        }

        /// <summary>
        /// 将base64转换成bitmap图片
        /// </summary>
        /// <param name="base64String"></param>
        /// <returns>bitmap</returns>
        public static Bitmap StringToBitmap(string base64String)
        {
            Bitmap bmpReturn = null;
            //Convert Base64 string to byte[]
            byte[] byteBuffer = Convert.FromBase64String(base64String);
            using (var memoryStream = new MemoryStream(byteBuffer))
            {
                memoryStream.Position = 0;
                bmpReturn = (Bitmap)Bitmap.FromStream(memoryStream);
                return bmpReturn;
            }
        }

        /// <summary>
        /// Bitmap 转 BitmapImage
        /// </summary>
        /// <param name="bitmap"></param>
        /// <returns></returns>
        public static BitmapImage BitmapToImageSource(Bitmap bitmap)
        {
            using (var memory = new MemoryStream())
            {
                //这里需要将原来的bitmap重新复制一份出来使用newBitmap进行Save 否则会报GDI+中出现一般性错误
                //https://stackoverflow.com/questions/15862810/a-generic-error-occurred-in-gdi-in-bitmap-save-method
                Bitmap newBitmap = new Bitmap(bitmap);
                bitmap.Dispose();
                bitmap = null;

                newBitmap.Save(memory, System.Drawing.Imaging.ImageFormat.Bmp);
                memory.Position = 0;
                BitmapImage bitmapImage = new BitmapImage();
                bitmapImage.BeginInit();
                bitmapImage.StreamSource = memory;
                bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                bitmapImage.EndInit();

                return bitmapImage;
            }
        }

        /// <summary>
        /// 复制文件
        /// </summary>
        /// <param name="localFilePaht">要复制的文件路径</param>
        /// <param name="saveFilePath">指定存储的路径</param>
        public static void CopyAndSaveImage(string localFilePaht, string saveFilePath)
        {
           
            if (File.Exists(localFilePaht))//必须判断要复制的文件是否存在
            {
                if (!Directory.Exists("Image"))
                    Directory.CreateDirectory("Image");
                File.Copy(localFilePaht, saveFilePath, true);//三个参数分别是源文件路径，存储路径，若存储路径有相同文件是否替换
            }
        }

        /// <summary>
        /// 删除文件夹以及文件
        /// </summary>
        /// <param name="directoryPath"> 文件夹路径 </param>
        /// <param name="fileName"> 文件名称 </param>
        public static void DeleteImage(string directoryPath, string fileName)
        {
            if (fileName == null)
                return;

            //删除文件
            for (int i = 0; i < Directory.GetFiles(directoryPath).ToList().Count; i++)
            {
                if (Directory.GetFiles(directoryPath)[i] == fileName)
                {
                    File.Delete(fileName);
                }
            }

            ////删除文件夹
            //for (int i = 0; i < Directory.GetDirectories(directoryPath).ToList().Count; i++)
            //{
            //    if (Directory.GetDirectories(directoryPath)[i] == fileName)
            //    {
            //        Directory.Delete(fileName, true);
            //    }
            //}
        }

        /// <summary>
        /// 截取全屏幕图像
        /// </summary>
        /// <returns>屏幕位图</returns>
        public static Image GetFullScreen(string filename = default)
        {
            // take screenshot from primary display only
            //Image screen = Pranas.ScreenshotCapture.TakeScreenshot(true);
            //screen.Save(EmguCvHelper.FullScreenImage);
            //return screen;

            Image screen = new Bitmap(Screen.PrimaryScreen.Bounds.Width, Screen.PrimaryScreen.Bounds.Height);
            Graphics g = Graphics.FromImage(screen);
            g.CopyFromScreen(new Point(0, 0), new Point(0, 0), Screen.PrimaryScreen.Bounds.Size);
            if (filename != default)
            {
                //screen.Save(@".\文件名.jpg", ImageFormat.Jpeg);
                screen.Save(filename);
            }
            return screen;
        }

        /// <summary>
        /// 截取特定屏幕区域图像
        /// </summary>
        /// <returns>屏幕位图</returns>
        public static Image GetAndSaveSpecificScreenArea(string filename, Rectangle RectArea)
        {
            //Image screen = new Bitmap(Screen.PrimaryScreen.Bounds.Width, Screen.PrimaryScreen.Bounds.Height);
            //Graphics g = Graphics.FromImage(screen);
            //g.CopyFromScreen(new Point(0, 0), new Point(0, 0), Screen.PrimaryScreen.Bounds.Size);
            //var PartialScreenImage = CaptureImage(screen, RectArea.X, RectArea.Y, RectArea.Width, RectArea.Height);
            //PartialScreenImage.Save(filename);
            //return screen;

            Image partialScreen = new Bitmap(RectArea.Width, RectArea.Height);
            Graphics g = Graphics.FromImage(partialScreen);
            g.CopyFromScreen(RectArea.X, RectArea.Y, 0, 0, RectArea.Size);

            partialScreen.Save(filename);
            //PartialScreenImage.Save(filename);
            return partialScreen;
        }

        /// <summary>
        /// 截取特定屏幕区域图像
        /// </summary>
        /// <returns>屏幕位图</returns>
        public static Image GetSpecificScreenArea(Rectangle RectArea)
        {
            Image partialScreen = new Bitmap(RectArea.Width, RectArea.Height);
            Graphics g = Graphics.FromImage(partialScreen);
            g.CopyFromScreen(RectArea.X, RectArea.Y, 0, 0, RectArea.Size);
            return partialScreen;
        }


        //public static bool ScreenshotByDx(string filename, out Image Img)
        //{
        //    //var dx = new DirectXScreenCapturer();
        //    //var (result, isBlackFrame, image) = dx.GetFrameImage();
        //    //Img = image;
        //    //if (result.Success && !isBlackFrame)
        //    //{
        //    //    image.Save(@"C:\Users\YR\111", ImageFormat.Png);
        //    //    return true;
        //    //}
        //    return false;
        //}



        /// <summary>
        /// 从大图中截取一部分图片
        /// </summary>
        /// <param name="fromImagePath">来源图片地址</param>        
        /// <param name="offsetX">从偏移X坐标位置开始截取</param>
        /// <param name="offsetY">从偏移Y坐标位置开始截取</param>
        /// <param name="width">保存图片的宽度</param>
        /// <param name="height">保存图片的高度</param>
        /// <returns></returns>
        public static System.Drawing.Image CaptureImage(System.Drawing.Image fromImage, int offsetX, int offsetY, int width, int height)
        {
            //创建新图位图
            Bitmap bitmap = new Bitmap(width, height, PixelFormat.Format32bppArgb);
            //创建作图区域
            Graphics graphic = Graphics.FromImage(bitmap);
            SolidBrush brush = new SolidBrush(Color.FromArgb(0, 255, 0, 0));  //A为透明度
            graphic.FillRectangle(brush, new Rectangle(0, 0, width, height));
            //截取原图相应区域写入作图区
            graphic.DrawImage(fromImage, 0, 0, new Rectangle(offsetX, offsetY, width, height), GraphicsUnit.Pixel);
            //从作图区生成新图
            System.Drawing.Image saveImage = System.Drawing.Image.FromHbitmap(bitmap.GetHbitmap(Color.Transparent));
            //保存图片
            //saveImage.Save(toImagePath, ImageFormat.Png);
            //释放资源   
            graphic.Dispose();
            bitmap.Dispose();
            //saveImage.Dispose();
            return saveImage;
        }

        /// <summary>
        /// 获取需要点击的位置坐标
        /// </summary>
        /// <param name="rect"></param>
        /// <param name="location"></param>
        /// <param name="offSet"></param>
        /// <returns></returns>
        public static Point GetClickPoint(Rectangle rect, MatchOptions MatOptions, ClickLocation location, Point offSet = default)
        {
            Point point;
            switch (location) 
            {
                case ClickLocation.Center: point = new Point(rect.X + rect.Width / 2, rect.Y + rect.Height / 2); break;

                case ClickLocation.LeftTop: point = new Point(rect.X, rect.Y); break;
                case ClickLocation.LeftCenter: point = new Point(rect.X, rect.Y + rect.Height / 2); break;
                case ClickLocation.LeftBottom: point = new Point(rect.X, rect.Y + rect.Height); break;

                case ClickLocation.CenterTop: point = new Point(rect.X + rect.Width / 2, rect.Y); break;
                case ClickLocation.CenterBottom: point = new Point(rect.X + rect.Width / 2, rect.Y + rect.Height); break;

                case ClickLocation.RightTop: point = new Point(rect.X + rect.Width, rect.Y); break;
                case ClickLocation.RightCenter: point = new Point(rect.X + rect.Width, rect.Y + rect.Height / 2); break;
                case ClickLocation.RightBottom: point = new Point(rect.X + rect.Width, rect.Y + rect.Height); break;

                default:return Point.Empty;
            }
            if(MatOptions.MatchMode == MatchMode.Relatively)
            {
                point.Offset(MatOptions.WindowArea.X, MatOptions.WindowArea.Y);
            }

            if (offSet != default)
            {
                point.Offset(offSet);
            }
            return point;
        }
    }


    public static class CaptureWindow
    {
        #region 类
        /// <summary>
        /// Helper class containing User32 API functions
        /// </summary>
        private class User32
        {
            [StructLayout(LayoutKind.Sequential)]
            public struct RECT
            {
                public int left;
                public int top;
                public int right;
                public int bottom;
            }
            [DllImport("user32.dll")]
            public static extern IntPtr GetDesktopWindow();
            [DllImport("user32.dll")]
            public static extern IntPtr GetWindowDC(IntPtr hWnd);
            [DllImport("user32.dll")]
            public static extern IntPtr ReleaseDC(IntPtr hWnd, IntPtr hDC);
            [DllImport("user32.dll")]
            public static extern IntPtr GetWindowRect(IntPtr hWnd, ref RECT rect);

            [DllImport("user32.dll", EntryPoint = "FindWindow", CharSet = CharSet.Unicode)]
            public static extern IntPtr FindWindow(string lpClassName, string lpWindowName);
        }

        private class Gdi32
        {

            public const int SRCCOPY = 0x00CC0020; // BitBlt dwRop parameter
            [DllImport("gdi32.dll")]
            public static extern bool BitBlt(IntPtr hObject, int nXDest, int nYDest,
            int nWidth, int nHeight, IntPtr hObjectSource,
            int nXSrc, int nYSrc, int dwRop);
            [DllImport("gdi32.dll")]
            public static extern IntPtr CreateCompatibleBitmap(IntPtr hDC, int nWidth,
            int nHeight);
            [DllImport("gdi32.dll")]
            public static extern IntPtr CreateCompatibleDC(IntPtr hDC);
            [DllImport("gdi32.dll")]
            public static extern bool DeleteDC(IntPtr hDC);
            [DllImport("gdi32.dll")]
            public static extern bool DeleteObject(IntPtr hObject);
            [DllImport("gdi32.dll")]
            public static extern IntPtr SelectObject(IntPtr hDC, IntPtr hObject);
        }
        #endregion

        /// <summary>
        /// 根据句柄截图
        /// </summary>
        /// <param name="hWnd">句柄</param>
        /// <returns></returns>
        public static Image ByHwnd(IntPtr hWnd)
        {
            // 获取目标窗口的DC
            IntPtr hdcSrc = User32.GetWindowDC(hWnd);
            // get the size
            User32.RECT windowRect = new User32.RECT();
            User32.GetWindowRect(hWnd, ref windowRect);
            int width = windowRect.right - windowRect.left;
            int height = windowRect.bottom - windowRect.top;

            //创建可以复制到的设备上下文
            IntPtr hdcDest = Gdi32.CreateCompatibleDC(hdcSrc);
            //创建可以复制到的位图，
            //使用Get Device Caps获取宽度/高度
            IntPtr hBitmap = Gdi32.CreateCompatibleBitmap(hdcSrc, width, height);

            // 选择位图对象
            IntPtr hOld = Gdi32.SelectObject(hdcDest, hBitmap);

            // bitblt over
            Gdi32.BitBlt(hdcDest, 0, 0, width, height, hdcSrc, 0, 0, Gdi32.SRCCOPY);
            // restore selection
            Gdi32.SelectObject(hdcDest, hOld);
            // clean up
            Gdi32.DeleteDC(hdcDest);
            User32.ReleaseDC(hWnd, hdcSrc);
            // get a .NET image object for it
            Image img = Image.FromHbitmap(hBitmap);
            // free up the Bitmap object
            Gdi32.DeleteObject(hBitmap);
            return img;
        }

        /// <summary>
        /// 根据窗口名称截图
        /// </summary>
        /// <param name="windowName">窗口名称</param>
        /// <returns></returns>
        public static Image ByName(string windowName)
        {
            IntPtr handle = User32.FindWindow(null, windowName);
            IntPtr hdcSrc = User32.GetWindowDC(handle);
            User32.RECT windowRect = new User32.RECT();
            User32.GetWindowRect(handle, ref windowRect);
            int width = windowRect.right - windowRect.left;
            int height = windowRect.bottom - windowRect.top;
            IntPtr hdcDest = Gdi32.CreateCompatibleDC(hdcSrc);
            IntPtr hBitmap = Gdi32.CreateCompatibleBitmap(hdcSrc, width, height);
            IntPtr hOld = Gdi32.SelectObject(hdcDest, hBitmap);
            Gdi32.BitBlt(hdcDest, 0, 0, width, height, hdcSrc, 0, 0, Gdi32.SRCCOPY);
            Gdi32.SelectObject(hdcDest, hOld);
            Gdi32.DeleteDC(hdcDest);
            User32.ReleaseDC(handle, hdcSrc);
            Image img = Image.FromHbitmap(hBitmap);
            Gdi32.DeleteObject(hBitmap);
            return img;
        }
    }

    //public class DirectXScreenCapturer : IDisposable
    //{
    //    private Factory1 factory;
    //    private Adapter1 adapter;
    //    private SharpDX.Direct3D12.Device device;
    //    private Output output;
    //    private Output1 output1;
    //    private Texture2DDescription textureDesc;
    //    private texture textureDesc;
    //    //2D 纹理，存储截屏数据
    //    private Texture2D screenTexture;

    //    public DirectXScreenCapturer()
    //    {
    //        // 获取输出设备（显卡、显示器），这里是主显卡和主显示器
    //        factory = new Factory1();
    //        adapter = factory.GetAdapter1(0);
    //        device = new SharpDX.Direct3D12.Device(adapter);
    //        output = adapter.GetOutput(0);
    //        output1 = output.QueryInterface<Output1>();

    //        //设置纹理信息，供后续使用（截图大小和质量）
    //        textureDesc = new Texture2DDescription
    //        {
    //            CpuAccessFlags = CpuAccessFlags.Read,
    //            BindFlags = BindFlags.None,
    //            Format = Format.B8G8R8A8_UNorm,
    //            Width = output.Description.DesktopBounds.Right,
    //            Height = output.Description.DesktopBounds.Bottom,
    //            OptionFlags = ResourceOptionFlags.None,
    //            MipLevels = 1,
    //            ArraySize = 1,
    //            SampleDescription = { Count = 1, Quality = 0 },
    //            Usage = ResourceUsage.Staging
    //        };

    //        screenTexture = new Texture2D(device, textureDesc);
    //    }

    //    public Result ProcessFrame(Action<DataBox, Texture2DDescription> processAction, int timeoutInMilliseconds = 5)
    //    {
    //        //截屏，可能失败
    //        using  OutputDuplication duplicatedOutput = output1.DuplicateOutput(device);
    //        var result = duplicatedOutput.TryAcquireNextFrame(timeoutInMilliseconds, out OutputDuplicateFrameInformation duplicateFrameInformation, out SharpDX.DXGI.Resource screenResource);
    //        if (!result.Success) return result;

    //        using Texture2D screenTexture2D = screenResource.QueryInterface<Texture2D>();

    //        //复制数据
    //        device.ImmediateContext.CopyResource(screenTexture2D, screenTexture);
    //        DataBox mapSource = device.ImmediateContext.MapSubresource(screenTexture, 0, MapMode.Read, SharpDX.Direct3D12.MapFlags.None);

    //        processAction?.Invoke(mapSource, textureDesc);

    //        //释放资源
    //        device.ImmediateContext.UnmapSubresource(screenTexture, 0);
    //        screenResource.Dispose();
    //        duplicatedOutput.ReleaseFrame();

    //        return result;
    //    }

    //    public (Result result, bool isBlackFrame, Image image) GetFrameImage(int timeoutInMilliseconds = 5)
    //    {
    //        //生成 C# 用图像
    //        Bitmap image = new Bitmap(textureDesc.Width, textureDesc.Height, PixelFormat.Format24bppRgb);
    //        bool isBlack = true;
    //        var result = ProcessFrame(ProcessImage);

    //        if (!result.Success) image.Dispose();

    //        return (result, isBlack, result.Success ? image : null);

    //        void ProcessImage(DataBox dataBox, Texture2DDescription texture)
    //        {
    //            BitmapData data = image.LockBits(new Rectangle(0, 0, texture.Width, texture.Height), ImageLockMode.ReadWrite, PixelFormat.Format24bppRgb);

    //            unsafe
    //            {
    //                byte* dataHead = (byte*)dataBox.DataPointer.ToPointer();

    //                for (int x = 0; x < texture.Width; x++)
    //                {
    //                    for (int y = 0; y < texture.Height; y++)
    //                    {
    //                        byte* pixPtr = (byte*)(data.Scan0 + y * data.Stride + x * 3);

    //                        int pos = x + y * texture.Width;
    //                        pos *= 4;

    //                        byte r = dataHead[pos + 2];
    //                        byte g = dataHead[pos + 1];
    //                        byte b = dataHead[pos + 0];

    //                        if (isBlack && (r != 0 || g != 0 || b != 0)) isBlack = false;

    //                        pixPtr[0] = b;
    //                        pixPtr[1] = g;
    //                        pixPtr[2] = r;
    //                    }
    //                }
    //            }

    //            image.UnlockBits(data);
    //        }
    //    }

    //    #region IDisposable Support
    //    private bool disposedValue = false; // 要检测冗余调用

    //    protected virtual void Dispose(bool disposing)
    //    {
    //        if (!disposedValue)
    //        {
    //            if (disposing)
    //            {
    //                // TODO: 释放托管状态(托管对象)。
    //                factory.Dispose();
    //                adapter.Dispose();
    //                device.Dispose();
    //                output.Dispose();
    //                output1.Dispose();
    //                screenTexture.Dispose();
    //            }

    //            // TODO: 释放未托管的资源(未托管的对象)并在以下内容中替代终结器。
    //            // TODO: 将大型字段设置为 null。
    //            factory = null;
    //            adapter = null;
    //            device = null;
    //            output = null;
    //            output1 = null;
    //            screenTexture = null;

    //            disposedValue = true;
    //        }
    //    }

    //    // TODO: 仅当以上 Dispose(bool disposing) 拥有用于释放未托管资源的代码时才替代终结器。
    //    // ~DirectXScreenCapturer()
    //    // {
    //    //   // 请勿更改此代码。将清理代码放入以上 Dispose(bool disposing) 中。
    //    //   Dispose(false);
    //    // }

    //    // 添加此代码以正确实现可处置模式。
    //    public void Dispose()
    //    {
    //        // 请勿更改此代码。将清理代码放入以上 Dispose(bool disposing) 中。
    //        Dispose(true);
    //        // TODO: 如果在以上内容中替代了终结器，则取消注释以下行。
    //        // GC.SuppressFinalize(this);
    //    }
    //    #endregion
    //}


}
