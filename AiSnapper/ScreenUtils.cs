using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Windows;
using System.Windows.Forms;

namespace AiSnapper
{
    public static class ScreenUtils
    {
        public static Rectangle VirtualScreenRect()
        {
            // Use Windows Forms SystemInformation.VirtualScreen which returns device pixels
            var vs = SystemInformation.VirtualScreen;
            return new Rectangle(vs.Left, vs.Top, vs.Width, vs.Height);
        }

        public static byte[] CaptureToPng(Rectangle rect)
        {
            // Ensure the capture rectangle is within virtual screen bounds
            var virtualScreen = VirtualScreenRect();
            rect = Rectangle.Intersect(rect, virtualScreen);
            
            if (rect.Width <= 0 || rect.Height <= 0)
                throw new ArgumentException("Invalid capture rectangle - outside of screen bounds");

            using var bmp = new Bitmap(rect.Width, rect.Height);
            using var g = Graphics.FromImage(bmp);
            g.CopyFromScreen(rect.Left, rect.Top, 0, 0, rect.Size, CopyPixelOperation.SourceCopy);
            using var ms = new MemoryStream();
            bmp.Save(ms, ImageFormat.Png);
            return ms.ToArray();
        }

        // New: Capture the entire virtual screen once and crop from it. This can be more robust across mixed-DPI setups.
        public static byte[] CaptureVirtualThenCropToPng(Rectangle rect)
        {
            var virtualScreen = VirtualScreenRect();
            using var full = new Bitmap(virtualScreen.Width, virtualScreen.Height);
            using (var g = Graphics.FromImage(full))
            {
                g.CopyFromScreen(virtualScreen.Left, virtualScreen.Top, 0, 0, full.Size, CopyPixelOperation.SourceCopy);
            }

            // Adjust rect to be relative to virtualScreen origin
            var rel = new Rectangle(rect.Left - virtualScreen.Left, rect.Top - virtualScreen.Top, rect.Width, rect.Height);
            rel = Rectangle.Intersect(rel, new Rectangle(0, 0, virtualScreen.Width, virtualScreen.Height));
            if (rel.Width <= 0 || rel.Height <= 0)
                throw new ArgumentException("Invalid capture rectangle - outside of screen bounds");

            using var cropped = full.Clone(rel, full.PixelFormat);
            using var ms = new MemoryStream();
            cropped.Save(ms, ImageFormat.Png);
            return ms.ToArray();
        }

        // Helper method to get screen containing a point
        public static Screen GetScreenFromPoint(System.Drawing.Point point)
        {
            return Screen.FromPoint(point);
        }

        // Helper method to get all available screens
        public static Screen[] GetAllScreens()
        {
            return Screen.AllScreens;
        }
    }
}
