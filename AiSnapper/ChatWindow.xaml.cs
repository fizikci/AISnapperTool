using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;

namespace AiSnapper
{
    public class ChatItem
    {
        public bool IsUser { get; set; }
        public string Text { get; set; } = string.Empty;
    }

    public partial class ChatWindow : Window
    {
        private readonly byte[] _png;
        private readonly ObservableCollection<ChatItem> _items = new();
        private readonly System.Collections.Generic.List<object> _messages = new();
        private DateTime _lastMouseMove = DateTime.UtcNow;

        public ChatWindow(byte[] png)
        {
            InitializeComponent();
            _png = png;
            ChatList.ItemsSource = _items;

            var img = new BitmapImage();
            img.BeginInit();
            img.StreamSource = new MemoryStream(_png);
            img.CacheOption = BitmapCacheOption.OnLoad;
            img.EndInit();
            PreviewImage.Source = img;

            // Add a system prompt for better responses about the provided image
            _messages.Add(new {
                role = "system",
                content = new object[] { new { type = "text", text = "You are a helpful assistant. When answering, be concise and reference the attached image when relevant." } }
            });
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

            // Add user message. First user message should include the image; subsequent ones can be text-only.
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
                _messages.Add(new {
                    role = "user",
                    content = new object[] { new { type = "text", text = prompt } }
                });
            }

            ShowSpinner(true);
            await Task.Yield();
            ChatScroll.ScrollToEnd();

            try
            {
                var reply = await OpenAIClient.AskAsync(_messages.ToArray());
                ShowSpinner(false);
                _items.Add(new ChatItem { IsUser = false, Text = reply });
                _messages.Add(new {
                    role = "assistant",
                    content = new object[] { new { type = "text", text = reply } }
                });
                await Task.Yield();
                ChatScroll.ScrollToEnd();
            }
            catch (Exception ex)
            {
                ShowSpinner(false);
                _items.Add(new ChatItem { IsUser = false, Text = $"Error: {ex.Message}" });
            }
        }

        private void ShowSpinner(bool visible)
        {
            if (TypingBubble == null) return;
            TypingBubble.Visibility = visible ? Visibility.Visible : Visibility.Collapsed;
        }

        private async void PromptBox_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == Key.Enter && Keyboard.Modifiers == ModifierKeys.None)
            {
                e.Handled = true;
                await SendCurrentAsync();
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
            _lastMouseMove = DateTime.UtcNow;
            if (HeaderBar.Opacity < 1)
            {
                var fadeIn = new DoubleAnimation(1, TimeSpan.FromMilliseconds(150));
                HeaderBar.BeginAnimation(OpacityProperty, fadeIn);
            }
            // schedule fade out if idle
            _ = ScheduleHeaderFadeOutAsync();
        }

        private async Task ScheduleHeaderFadeOutAsync()
        {
            await Task.Delay(1800);
            var idleFor = DateTime.UtcNow - _lastMouseMove;
            if (idleFor.TotalMilliseconds >= 1600)
            {
                var fadeOut = new DoubleAnimation(0.15, TimeSpan.FromMilliseconds(300));
                HeaderBar.BeginAnimation(OpacityProperty, fadeOut);
            }
        }
    }
}
