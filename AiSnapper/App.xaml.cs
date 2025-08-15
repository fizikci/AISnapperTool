using System;
using System.Runtime.InteropServices;
using System.Windows;

namespace AiSnapper
{
    public partial class App : System.Windows.Application
    {
        [DllImport("kernel32.dll")]
        static extern bool AllocConsole();

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            
            // Allocate a console for this WPF application
            AllocConsole();
            Console.WriteLine("AiSnapper Debug Console");
        }
    }
}
