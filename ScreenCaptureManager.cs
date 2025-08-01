using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Windows.Forms;

namespace Blueshot
{
    public class ScreenCaptureManager
    {
        public Bitmap CaptureFullScreen()
        {
            var bounds = GetVirtualScreenBounds();
            return CaptureRegion(bounds);
        }

        public Bitmap CaptureRegion(Rectangle region)
        {
            try
            {
                if (region.IsEmpty || region.Width <= 0 || region.Height <= 0)
                {
                    throw new ArgumentException("Invalid capture region specified");
                }

                var bitmap = new Bitmap(region.Width, region.Height, PixelFormat.Format32bppArgb);
                
                using (var graphics = Graphics.FromImage(bitmap))
                {
                    graphics.CopyFromScreen(region.X, region.Y, 0, 0, region.Size, CopyPixelOperation.SourceCopy);
                }

                return bitmap;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to capture screen region: {ex.Message}", ex);
            }
        }

        public Bitmap CaptureWindow(IntPtr windowHandle)
        {
            try
            {
                var windowRect = GetWindowRectangle(windowHandle);
                return CaptureRegion(windowRect);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to capture window: {ex.Message}", ex);
            }
        }

        public Bitmap CaptureActiveWindow()
        {
            try
            {
                var activeWindow = GetForegroundWindow();
                if (activeWindow == IntPtr.Zero)
                {
                    throw new InvalidOperationException("No active window found");
                }

                return CaptureWindow(activeWindow);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to capture active window: {ex.Message}", ex);
            }
        }

        private Rectangle GetVirtualScreenBounds()
        {
            int left = int.MaxValue;
            int top = int.MaxValue;
            int right = int.MinValue;
            int bottom = int.MinValue;

            foreach (Screen screen in Screen.AllScreens)
            {
                left = Math.Min(left, screen.Bounds.Left);
                top = Math.Min(top, screen.Bounds.Top);
                right = Math.Max(right, screen.Bounds.Right);
                bottom = Math.Max(bottom, screen.Bounds.Bottom);
            }

            return new Rectangle(left, top, right - left, bottom - top);
        }

        private Rectangle GetWindowRectangle(IntPtr windowHandle)
        {
            if (GetWindowRect(windowHandle, out RECT rect))
            {
                return new Rectangle(rect.Left, rect.Top, 
                    rect.Right - rect.Left, rect.Bottom - rect.Top);
            }
            
            throw new InvalidOperationException("Unable to get window rectangle");
        }

        #region Windows API

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

        [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
        private struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        #endregion
    }

    public enum CaptureType
    {
        Region,
        FullScreen,
        ActiveWindow,
        Window
    }

    public class CaptureSettings
    {
        public CaptureType Type { get; set; } = CaptureType.Region;
        public ImageFormat OutputFormat { get; set; } = ImageFormat.Png;
        public int Quality { get; set; } = 95; // For JPEG compression
        public bool IncludeCursor { get; set; } = false;
        public string DefaultSavePath { get; set; } = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
        public bool AutoSave { get; set; } = false;
        public string FileNamePattern { get; set; } = "Screenshot_{timestamp}";
        
        public string GenerateFileName()
        {
            var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            var fileName = FileNamePattern.Replace("{timestamp}", timestamp);
            
            var extension = OutputFormat == ImageFormat.Jpeg ? ".jpg" : 
                           OutputFormat == ImageFormat.Png ? ".png" : 
                           OutputFormat == ImageFormat.Bmp ? ".bmp" : 
                           OutputFormat == ImageFormat.Gif ? ".gif" : ".png";
            
            return fileName + extension;
        }
    }
}
