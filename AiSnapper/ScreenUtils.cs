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
            return new Rectangle(
                (int)SystemParameters.VirtualScreenLeft,
                (int)SystemParameters.VirtualScreenTop,
                (int)SystemParameters.VirtualScreenWidth,
                (int)SystemParameters.VirtualScreenHeight
            );
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
            g.CopyFromScreen(rect.Left, rect.Top, 0, 0, rect.Size);
            using var ms = new MemoryStream();
            bmp.Save(ms, ImageFormat.Png);
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
