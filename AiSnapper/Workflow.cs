using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Threading.Tasks;
using System.Windows;

namespace AiSnapper
{
    public static class Workflow
    {
        public static async Task SelectRegionThenPromptAsync()
        {
            // Overlay to pick region
            var overlay = new SelectionOverlayWindow();
            var ok = overlay.ShowDialog();
            if (ok != true) return;

            Rectangle captureRect;
            if (overlay.SelectedRect.HasValue)
            {
                var r = overlay.SelectedRect.Value;
                // Convert overlay coordinates to screen coordinates
                // Since the overlay window covers the virtual screen, we need to add the virtual screen offset
                captureRect = new Rectangle((int)(r.X),
                                            (int)(r.Y),
                                            (int)r.Width, (int)r.Height);
            }
            else
            {
                captureRect = ScreenUtils.VirtualScreenRect();
            }

            await RunPromptFlowAsync(captureRect);
        }

        public static async Task FullScreenThenPromptAsync()
        {
            await RunPromptFlowAsync(ScreenUtils.VirtualScreenRect());
        }

        private static async Task RunPromptFlowAsync(Rectangle rect)
        {
            var png = ScreenUtils.CaptureToPng(rect);
            var preview = new PreviewDialog(png);
            var ok = preview.ShowDialog();
            if (ok != true) return;

            var b64 = Convert.ToBase64String(png);
            string reply;
            try
            {
                reply = await OpenAIClient.AskAsync(preview.PromptText!, b64);
            }
            catch (Exception ex)
            {
                reply = $"Error: {ex.Message}";
            }
            new ResultWindow(reply).ShowDialog();
        }
    }
}
