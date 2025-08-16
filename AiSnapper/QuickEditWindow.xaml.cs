using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace AiSnapper
{
    public partial class QuickEditWindow : Window
    {
        private IntPtr _targetHwnd;
        private string _originalText = string.Empty;
        private System.Windows.Controls.TextBox EditorBox => (System.Windows.Controls.TextBox)FindName("Editor");

        public QuickEditWindow(IntPtr targetHwnd, string text)
        {
            InitializeComponent();
            _targetHwnd = targetHwnd;
            _originalText = text;
            EditorBox.Text = text;
        }

        public static async Task ShowForSelectionAsync()
        {
            // Get the app that currently had focus before we act
            var target = NativeInput.GetForegroundWindow();
            if (target == IntPtr.Zero)
            {
                return;
            }

            // Explicitly copy from that window to avoid our own window stealing focus
            var copied = await NativeInput.TryCopySelectedTextAsync(target);
            if (string.IsNullOrWhiteSpace(copied))
            {
                return;
            }

            var w = new QuickEditWindow(target, copied);

            // Place near mouse cursor and clamp to the screen working area
            var p = System.Windows.Forms.Control.MousePosition;
            var screen = System.Windows.Forms.Screen.FromPoint(p);
            var wa = screen.WorkingArea;
            double width = w.Width;
            double height = w.Height;
            double left = p.X - width / 2;
            double top = p.Y + 20;
            if (left < wa.Left) left = wa.Left;
            if (top < wa.Top) top = wa.Top;
            if (left + width > wa.Right) left = wa.Right - width;
            if (top + height > wa.Bottom) top = Math.Max(wa.Top, wa.Bottom - height);
            w.Left = left;
            w.Top = top;
            w.Show();
            w.Activate();
        }

        private async Task TransformAsync(string instruction)
        {
            try
            {
                var messages = new object[]
                {
                    new { role = "system", content = new object[] { new { type = "text", text = "You are a writing assistant. Only return the transformed text without quotes." } } },
                    new { role = "user", content = new object[] { new { type = "text", text = $"Instruction: {instruction}\n\nText:\n{EditorBox.Text}" } } }
                };
                var result = await OpenAIClient.AskAsync(messages);
                if (!string.IsNullOrWhiteSpace(result))
                {
                    EditorBox.Text = result.Trim();
                    EditorBox.CaretIndex = EditorBox.Text.Length;
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(this, ex.Message, "AI Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void Expand_Click(object sender, RoutedEventArgs e) => await TransformAsync("Expand and elaborate while keeping the original meaning.");
        private async void Summarize_Click(object sender, RoutedEventArgs e) => await TransformAsync("Summarize concisely.");
        private async void RephrasePro_Click(object sender, RoutedEventArgs e) => await TransformAsync("Rephrase to a professional tone.");
        private async void RephraseCasual_Click(object sender, RoutedEventArgs e) => await TransformAsync("Rephrase to a casual, friendly tone.");

        private async void Accept_Click(object sender, RoutedEventArgs e)
        {
            var text = EditorBox.Text;
            await NativeInput.PasteTextAsync(text, _targetHwnd);
            Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void Window_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                Close();
            }
        }
    }
}
