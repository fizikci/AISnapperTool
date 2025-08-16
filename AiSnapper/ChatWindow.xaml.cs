using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;

namespace AiSnapper
{
    public class ChatItem : INotifyPropertyChanged
    {
        private bool _isUser;
        private string _text = string.Empty;

        public bool IsUser
        {
            get => _isUser;
            set { if (_isUser != value) { _isUser = value; OnPropertyChanged(); } }
        }

        public string Text
        {
            get => _text;
            set { if (_text != value) { _text = value; OnPropertyChanged(); } }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    public partial class ChatWindow : Window
    {
        private byte[]? _png;
        private readonly ObservableCollection<ChatItem> _items = new();
        private readonly System.Collections.Generic.List<object> _messages = new();
        private DateTime _lastMouseMove = DateTime.UtcNow;
        private CancellationTokenSource? _cts;

        // Added: parameterless constructor required by WPF XAML loader (used by StartupUri)
        public ChatWindow() : this(null) { }

        public ChatWindow(byte[]? png = null)
        {
            InitializeComponent();
            _png = png;
            ChatList.ItemsSource = _items;

            if (_png != null)
            {
                SetPreview(_png);
            }

            // Add a system prompt for better responses about the provided image
            _messages.Add(new {
                role = "system",
                content = new object[] { new { type = "text", text = "You are a helpful assistant. When answering, be concise and reference the attached image when relevant." } }
            });
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            if (PresentationSource.FromVisual(this) is HwndSource hs)
            {
                hs.AddHook(WndProc);
                TryEnableMica(hs.Handle);
            }
        }

        private void SetPreview(byte[] png)
        {
            var img = new BitmapImage();
            img.BeginInit();
            img.StreamSource = new MemoryStream(png);
            img.CacheOption = BitmapCacheOption.OnLoad;
            img.EndInit();
            PreviewImage.Source = img;
            PreviewImage.Visibility = Visibility.Visible;
            AddCapture.Visibility = Visibility.Collapsed;
        }

        // Context menu: copy the displayed image to clipboard
        private void CopyImage_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (PreviewImage.Source is BitmapSource bmp)
                {
                    System.Windows.Clipboard.SetImage(bmp);
                }
            }
            catch { }
        }

        // Context menu: start a new capture (same as clicking +)
        private async void NewCapture_Click(object sender, RoutedEventArgs e)
        {
            Hide();
            try
            {
                await Task.Delay(100);
                await CaptureSelectionAsync();
            }
            finally
            {
                Show();
                Activate();
            }
        }

        private Task<bool> CaptureSelectionAsync()
        {
            // Show overlay to pick region
            var overlay = new SelectionOverlayWindow();
            var ok = overlay.ShowDialog();
            if (ok != true)
                return Task.FromResult(false);

            System.Drawing.Rectangle captureRect;
            if (overlay.SelectedRectPx.HasValue)
            {
                captureRect = overlay.SelectedRectPx.Value;
            }
            else if (overlay.SelectedRect.HasValue)
            {
                var r = overlay.SelectedRect.Value;
                captureRect = new System.Drawing.Rectangle((int)(r.X + SystemParameters.VirtualScreenLeft),
                                                           (int)(r.Y + SystemParameters.VirtualScreenTop),
                                                           (int)r.Width, (int)r.Height);
            }
            else
            {
                captureRect = ScreenUtils.VirtualScreenRect();
            }

            var png = ScreenUtils.CaptureVirtualThenCropToPng(captureRect);
            _png = png;
            SetPreview(png);
            return Task.FromResult(true);
        }

        private async void AddCapture_Click(object sender, MouseButtonEventArgs e)
        {
            Hide();
            try
            {
                await Task.Delay(100); // allow window to hide before overlay shows
                await CaptureSelectionAsync();
            }
            finally
            {
                Show();
                Activate();
            }
        }

        private async void Send_Click(object sender, RoutedEventArgs e)
        {
            await SendCurrentAsync();
        }

        private async Task SendCurrentAsync()
        {
            var prompt = PromptBox.Text.Trim();
            if (string.IsNullOrWhiteSpace(prompt)) return;

            _items.Add(new ChatItem { IsUser = true, Text = prompt });
            PromptBox.Text = string.Empty;

            if (_png == null)
            {
                _items.Add(new ChatItem { IsUser = false, Text = "Click the + Capture button above to add a screenshot first." });
                return;
            }

            if (!_messages.Any(m => (string)m.GetType().GetProperty("role")!.GetValue(m)! == "user"))
            {
                _messages.Add(new {
                    role = "user",
                    content = new object[] {
                        new { type = "text", text = prompt },
                        new { type = "image_url", image_url = new { url = $"data:image/png;base64,{Convert.ToBase64String(_png)}", detail = "high" } }
                    }
                });
            }
            else
            {
                _messages.Add(new { role = "user", content = new object[] { new { type = "text", text = prompt } } });
            }

            await Task.Yield();
            ChatScroll.ScrollToEnd();

            var assistant = new ChatItem { IsUser = false, Text = string.Empty };
            _items.Add(assistant);

            _cts?.Cancel();
            _cts = new CancellationTokenSource();
            var ct = _cts.Token;

            try
            {
                await OpenAIClient.AskStreamAsync(
                    _messages.ToArray(),
                    onDelta: delta =>
                    {
                        Dispatcher.Invoke(() =>
                        {
                            assistant.Text += delta; // INotifyPropertyChanged will update UI
                            ChatScroll.ScrollToEnd();
                        });
                    },
                    onCompleted: () =>
                    {
                        Dispatcher.Invoke(() =>
                        {
                            _messages.Add(new { role = "assistant", content = new object[] { new { type = "text", text = assistant.Text } } });
                        });
                    },
                    ct: ct
                );
            }
            catch (Exception ex)
            {
                assistant.Text = $"Error: {ex.Message}";
            }
        }

        private void PromptBox_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == Key.Enter && Keyboard.Modifiers == ModifierKeys.None)
            {
                e.Handled = true;
                _ = SendCurrentAsync();
            }
        }

        private void PromptBox_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                if ((Keyboard.Modifiers & ModifierKeys.Shift) == ModifierKeys.Shift)
                {
                    // Allow newline
                    e.Handled = false;
                }
                else
                {
                    e.Handled = true;
                    _ = SendCurrentAsync(); // fire and forget
                }
            }
        }

        // Optional: Streaming placeholder to simulate incremental updates until real API streaming is wired
        private async Task SendCurrentAsyncStreaming()
        {
            var prompt = PromptBox.Text.Trim();
            if (string.IsNullOrWhiteSpace(prompt)) return;

            _items.Add(new ChatItem { IsUser = true, Text = prompt });
            PromptBox.Text = string.Empty;

            if (_png == null)
            {
                _items.Add(new ChatItem { IsUser = false, Text = "Click the + Capture button above to add a screenshot first." });
                return;
            }

            if (!_messages.Any(m => (string)m.GetType().GetProperty("role")!.GetValue(m)! == "user"))
            {
                _messages.Add(new {
                    role = "user",
                    content = new object[] {
                        new { type = "text", text = prompt },
                        new { type = "image_url", image_url = new { url = $"data:image/png;base64,{Convert.ToBase64String(_png)}", detail = "high" } }
                    }
                });
            }
            else
            {
                _messages.Add(new { role = "user", content = new object[] { new { type = "text", text = prompt } } });
            }

            await Task.Yield();
            ChatScroll.ScrollToEnd();

            // Add placeholder assistant item we will append to
            var assistant = new ChatItem { IsUser = false, Text = string.Empty };
            _items.Add(assistant);

            try
            {
                // For now, call the non-streaming API, then append progressively to the UI
                var full = await OpenAIClient.AskAsync(_messages.ToArray());
                // Simulate streaming by chunking
                const int chunk = 80;
                for (int i = 0; i < full.Length; i += chunk)
                {
                    assistant.Text += full.Substring(i, Math.Min(chunk, full.Length - i));
                    await Task.Delay(20);
                }

                _messages.Add(new { role = "assistant", content = new object[] { new { type = "text", text = full } } });
                await Task.Yield();
                ChatScroll.ScrollToEnd();
            }
            catch (Exception ex)
            {
                assistant.Text = $"Error: {ex.Message}";
            }
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void Header_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (e.ButtonState == MouseButtonState.Pressed)
                DragMove();
        }

        private void Window_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (HeaderBar.Opacity < 1)
            {
                var fadeIn = new DoubleAnimation(1, TimeSpan.FromMilliseconds(150));
                HeaderBar.BeginAnimation(OpacityProperty, fadeIn);
            }
        }

        // Edge resize support for borderless window
        private const int WM_NCHITTEST = 0x0084;
        private const int HTCLIENT = 1;
        private const int HTLEFT = 10, HTRIGHT = 11, HTTOP = 12, HTTOPLEFT = 13, HTTOPRIGHT = 14, HTBOTTOM = 15, HTBOTTOMLEFT = 16, HTBOTTOMRIGHT = 17;

        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == WM_NCHITTEST)
            {
                var mouseX = (short)((long)lParam & 0xFFFF);
                var mouseY = (short)(((long)lParam >> 16) & 0xFFFF);
                var pt = PointFromScreen(new System.Windows.Point(mouseX, mouseY));
                const int border = 8;
                if (pt.Y < border)
                {
                    if (pt.X < border) { handled = true; return new IntPtr(HTTOPLEFT); }
                    if (pt.X > ActualWidth - border) { handled = true; return new IntPtr(HTTOPRIGHT); }
                    handled = true; return new IntPtr(HTTOP);
                }
                if (pt.Y > ActualHeight - border)
                {
                    if (pt.X < border) { handled = true; return new IntPtr(HTBOTTOMLEFT); }
                    if (pt.X > ActualWidth - border) { handled = true; return new IntPtr(HTBOTTOMRIGHT); }
                    handled = true; return new IntPtr(HTBOTTOM);
                }
                if (pt.X < border) { handled = true; return new IntPtr(HTLEFT); }
                if (pt.X > ActualWidth - border) { handled = true; return new IntPtr(HTRIGHT); }
                return new IntPtr(HTCLIENT);
            }
            return IntPtr.Zero;
        }

        // Try to enable Mica backdrop on Windows 11
        private void TryEnableMica(IntPtr hwnd)
        {
            try
            {
                int TRUE = 1;
                int DWMWA_USE_IMMERSIVE_DARK_MODE = 20; // bool
                DwmSetWindowAttribute(hwnd, DWMWA_USE_IMMERSIVE_DARK_MODE, ref TRUE, sizeof(int));

                int DWMWA_SYSTEMBACKDROP_TYPE = 38; // int
                int DWMSBT_MAINWINDOW = 2;
                DwmSetWindowAttribute(hwnd, DWMWA_SYSTEMBACKDROP_TYPE, ref DWMSBT_MAINWINDOW, sizeof(int));
            }
            catch { /* best effort */ }
        }

        [DllImport("dwmapi.dll")]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);
    }
}
