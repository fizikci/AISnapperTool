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
            ShowSpinner(true);
            await Task.Yield();
            ChatScroll.ScrollToEnd();

            try
            {
                var b64 = Convert.ToBase64String(_png);
                var reply = await OpenAIClient.AskAsync(prompt, b64);
                ShowSpinner(false);
                _items.Add(new ChatItem { IsUser = false, Text = reply });
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
            // Shift+Enter inserts newline (default behavior allowed)
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
