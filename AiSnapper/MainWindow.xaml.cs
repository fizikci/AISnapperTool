using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace AiSnapper
{
    public partial class MainWindow : Window
    {
        private HotkeyManager? _hotkey;

        public MainWindow()
        {
            InitializeComponent();
            Loaded += MainWindow_Loaded;
            Closed += (_, __) => _hotkey?.Dispose();
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            var hwnd = new WindowInteropHelper(this).Handle;
            _hotkey = new HotkeyManager(hwnd);
            // Ctrl + Alt + I
            _hotkey.Register(Modifiers.MOD_CONTROL | Modifiers.MOD_ALT, VirtualKeys.I, OnHotkey);
        }

        private async void OnHotkey()
        {
            await Workflow.SelectRegionThenPromptAsync();
        }

        private async void FullScreen_Click(object sender, RoutedEventArgs e)
        {
            await Workflow.FullScreenThenPromptAsync();
        }

        private async void Region_Click(object sender, RoutedEventArgs e)
        {
            await Workflow.SelectRegionThenPromptAsync();
        }
    }
}
