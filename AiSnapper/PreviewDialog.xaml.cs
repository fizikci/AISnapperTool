using System;
using System.IO;
using System.Windows;
using System.Windows.Media.Imaging;

namespace AiSnapper
{
    public partial class PreviewDialog : Window
    {
        private readonly byte[] _png;
        public string? PromptText { get; private set; }

        public PreviewDialog(byte[] pngBytes)
        {
            InitializeComponent();
            _png = pngBytes;
            var img = new BitmapImage();
            img.BeginInit();
            img.StreamSource = new MemoryStream(_png);
            img.CacheOption = BitmapCacheOption.OnLoad;
            img.EndInit();
            PreviewImage.Source = img;
        }

        private void Send_Click(object sender, RoutedEventArgs e)
        {
            PromptText = PromptBox.Text.Trim();
            if (string.IsNullOrWhiteSpace(PromptText))
            {
                System.Windows.MessageBox.Show("Please enter a prompt.", "AiSnapper", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            DialogResult = true;
            Close();
        }
    }
}
