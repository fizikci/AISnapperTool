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
        public static Task SelectRegionThenPromptAsync()
        {
            // Overlay to pick region
            var overlay = new SelectionOverlayWindow();
            var ok = overlay.ShowDialog();
            if (ok != true) return Task.CompletedTask;

            Rectangle captureRect;
            if (overlay.SelectedRectPx.HasValue)
            {
                // Use device-pixel rectangle computed by the overlay
                captureRect = overlay.SelectedRectPx.Value;
            }
            else if (overlay.SelectedRect.HasValue)
            {
                // Legacy fallback path (should rarely be used now)
                var r = overlay.SelectedRect.Value;
                captureRect = new Rectangle((int)(r.X + SystemParameters.VirtualScreenLeft),
                                            (int)(r.Y + SystemParameters.VirtualScreenTop),
                                            (int)r.Width, (int)r.Height);
            }
            else
            {
                captureRect = ScreenUtils.VirtualScreenRect();
            }

            // Capture entire virtual screen first, then crop selection for robustness
            var png = ScreenUtils.CaptureVirtualThenCropToPng(captureRect);

            // Open modern chat window with preview
            var chat = new ChatWindow(png);
            chat.ShowDialog();
            return Task.CompletedTask;
        }

        public static Task FullScreenThenPromptAsync()
        {
            // Capture full virtual screen
            var png = ScreenUtils.CaptureVirtualThenCropToPng(ScreenUtils.VirtualScreenRect());
            var chat = new ChatWindow(png);
            chat.ShowDialog();
            return Task.CompletedTask;
        }

        private static async Task RunPromptFlowAsync(Rectangle rect)
        {
            // Legacy path no longer used; retained for compatibility
            var png = ScreenUtils.CaptureVirtualThenCropToPng(rect);
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
