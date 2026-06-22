using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SkipDrama_YuanShen
{
    internal sealed class DirectScreenshotService : IDisposable
    {
        private readonly SemaphoreSlim _captureGate = new SemaphoreSlim(1, 1);
        private readonly string _folder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.MyVideos),
            "GamePadScreenshot");

        internal async Task<string> CapturePrimaryScreenAsync(CancellationToken token)
        {
            await _captureGate.WaitAsync(token).ConfigureAwait(false);
            try
            {
                return await Task.Run(() => CapturePrimaryScreen(token), token).ConfigureAwait(false);
            }
            finally
            {
                _captureGate.Release();
            }
        }

        private string CapturePrimaryScreen(CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            Directory.CreateDirectory(_folder);

            var primaryScreen = Screen.PrimaryScreen;
            if (primaryScreen == null)
            {
                throw new InvalidOperationException("没有找到主显示器。");
            }

            var bounds = primaryScreen.Bounds;
            using (var bitmap = new Bitmap(bounds.Width, bounds.Height, PixelFormat.Format32bppArgb))
            using (var graphics = Graphics.FromImage(bitmap))
            {
                graphics.CopyFromScreen(bounds.Left, bounds.Top, 0, 0, bounds.Size);
                var filename = "Screenshot_" + DateTime.Now.ToString("yyyyMMdd_HHmmss_fff") + "_" + Guid.NewGuid().ToString("N").Substring(0, 6) + ".png";
                var path = Path.Combine(_folder, filename);
                bitmap.Save(path, ImageFormat.Png);
                return path;
            }
        }

        public void Dispose()
        {
            _captureGate.Dispose();
        }
    }
}
