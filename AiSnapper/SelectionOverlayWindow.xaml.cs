using System;
using System.Diagnostics;
using System.Drawing;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Shapes;
using System.Windows.Media;

namespace AiSnapper
{
    public partial class SelectionOverlayWindow : Window
    {
        private System.Windows.Point _start;
        private bool _dragging;

        public Rect? SelectedRect { get; private set; }

        public SelectionOverlayWindow()
        {
            InitializeComponent();
            
            // Set window to cover all screens (virtual screen bounds)
            Left = SystemParameters.VirtualScreenLeft;
            Top = SystemParameters.VirtualScreenTop;
            Width = SystemParameters.VirtualScreenWidth;
            Height = SystemParameters.VirtualScreenHeight;
            WindowState = WindowState.Normal; // Don't use Maximized as it only covers primary screen
            
            MouseDown += OnMouseDown;
            MouseMove += OnMouseMove;
            MouseUp += OnMouseUp;
            KeyDown += (s, e) => { if (e.Key == Key.Escape) { DialogResult = false; Close(); } };
            MouseRightButtonUp += (s, e) => { SelectedRect = null; DialogResult = true; Close(); }; // full screen shortcut
        }

        private void OnMouseDown(object sender, MouseButtonEventArgs e)
        {
            _start = e.GetPosition(this);
            _dragging = true;
            SelectionRect.Visibility = Visibility.Visible;
            Canvas.SetLeft(SelectionRect, _start.X);
            Canvas.SetTop(SelectionRect, _start.Y);
            SelectionRect.Width = 0;
            SelectionRect.Height = 0;
        }

        private void OnMouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (!_dragging) return;
            var pos = e.GetPosition(this);
            var x = Math.Min(pos.X, _start.X);
            var y = Math.Min(pos.Y, _start.Y);
            var w = Math.Abs(pos.X - _start.X);
            var h = Math.Abs(pos.Y - _start.Y);
            Canvas.SetLeft(SelectionRect, x);
            Canvas.SetTop(SelectionRect, y);
            SelectionRect.Width = w;
            SelectionRect.Height = h;
        }

        private void OnMouseUp(object sender, MouseButtonEventArgs e)
        {
            if (!_dragging) return;
            _dragging = false;
            var x = Canvas.GetLeft(SelectionRect);
            var y = Canvas.GetTop(SelectionRect);
            var w = SelectionRect.Width;
            var h = SelectionRect.Height;

            Console.WriteLine($"Selection: x={x}, y={y}, w={w}, h={h}");
            Debug.WriteLine($"Selection: x={x}, y={y}, w={w}, h={h}");

            if (w > 5 && h > 5)
            {
                SelectedRect = new Rect(x, y, w, h);
                DialogResult = true;
                Close();
            }
            else
            {
                DialogResult = false;
                Close();
            }
        }
    }
}
