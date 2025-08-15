using System.Windows;

namespace AiSnapper
{
    public partial class ResultWindow : Window
    {
        public ResultWindow(string text)
        {
            InitializeComponent();
            OutputBox.Text = text;
        }

        private void Copy_Click(object sender, RoutedEventArgs e)
        {
            Clipboard.SetText(OutputBox.Text);
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
