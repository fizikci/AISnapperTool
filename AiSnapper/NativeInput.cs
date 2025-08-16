using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace AiSnapper
{
    internal static class NativeInput
    {
        private const uint KEYEVENTF_KEYUP = 0x0002;
        private const byte VK_CONTROL = 0x11;
        private const byte VK_MENU = 0x12; // Alt
        private const byte VK_C = 0x43;
        private const byte VK_V = 0x56;

        [DllImport("user32.dll")] private static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, UIntPtr dwExtraInfo);
        [DllImport("user32.dll")] public static extern IntPtr GetForegroundWindow();
        [DllImport("user32.dll")] private static extern bool SetForegroundWindow(IntPtr hWnd);
        [DllImport("user32.dll")] private static extern short GetAsyncKeyState(int vKey);

        private static bool IsKeyDown(int vKey) => (GetAsyncKeyState(vKey) & 0x8000) != 0;

        private static async Task EnsureModifiersUpAsync(int timeoutMs = 300)
        {
            var start = Environment.TickCount;
            while (IsKeyDown(VK_CONTROL) || IsKeyDown(VK_MENU))
            {
                if (Environment.TickCount - start > timeoutMs) break;
                await Task.Delay(10);
            }
            // Safety: if still down (user holding), briefly send key-up so our synthetic Ctrl+C isn't Ctrl+Alt+C
            if (IsKeyDown(VK_MENU)) keybd_event(VK_MENU, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);
            if (IsKeyDown(VK_CONTROL)) keybd_event(VK_CONTROL, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);
            await Task.Delay(10);
        }

        public static void SendCtrlC()
        {
            keybd_event(VK_CONTROL, 0, 0, UIntPtr.Zero);
            keybd_event(VK_C, 0, 0, UIntPtr.Zero);
            keybd_event(VK_C, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);
            keybd_event(VK_CONTROL, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);
        }

        public static void SendCtrlV()
        {
            keybd_event(VK_CONTROL, 0, 0, UIntPtr.Zero);
            keybd_event(VK_V, 0, 0, UIntPtr.Zero);
            keybd_event(VK_V, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);
            keybd_event(VK_CONTROL, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);
        }

        public static bool TryRestoreForegroundWindow(IntPtr hwnd)
        {
            if (hwnd == IntPtr.Zero) return false;
            return SetForegroundWindow(hwnd);
        }

        public static async Task<string?> TryCopySelectedTextAsync(int attempts = 4, int delayMs = 80)
        {
            string? result = null;
            var original = System.Windows.Clipboard.GetDataObject();

            for (int i = 0; i < attempts; i++)
            {
                try
                {
                    await EnsureModifiersUpAsync();
                    SendCtrlC();
                    await Task.Delay(delayMs);
                    if (System.Windows.Clipboard.ContainsText())
                    {
                        result = System.Windows.Clipboard.GetText();
                        break;
                    }
                }
                catch
                {
                    await Task.Delay(delayMs);
                }
            }

            // Restore previous clipboard
            try
            {
                if (original != null)
                {
                    System.Windows.Clipboard.SetDataObject(original, true);
                }
            }
            catch { }

            return string.IsNullOrWhiteSpace(result) ? null : result;
        }

        public static async Task<string?> TryCopySelectedTextAsync(IntPtr targetHwnd, int attempts = 4, int delayMs = 80)
        {
            if (targetHwnd == IntPtr.Zero)
            {
                return await TryCopySelectedTextAsync(attempts, delayMs);
            }

            string? result = null;
            var original = System.Windows.Clipboard.GetDataObject();

            for (int i = 0; i < attempts; i++)
            {
                try
                {
                    TryRestoreForegroundWindow(targetHwnd);
                    await Task.Delay(30);
                    await EnsureModifiersUpAsync();
                    SendCtrlC();
                    await Task.Delay(delayMs);
                    if (System.Windows.Clipboard.ContainsText())
                    {
                        result = System.Windows.Clipboard.GetText();
                        break;
                    }
                }
                catch
                {
                    await Task.Delay(delayMs);
                }
            }

            // Restore previous clipboard
            try
            {
                if (original != null)
                {
                    System.Windows.Clipboard.SetDataObject(original, true);
                }
            }
            catch { }

            return string.IsNullOrWhiteSpace(result) ? null : result;
        }

        public static async Task<bool> PasteTextAsync(string text, IntPtr targetHwnd)
        {
            var original = System.Windows.Clipboard.GetDataObject();
            try
            {
                System.Windows.Clipboard.SetText(text);
            }
            catch
            {
                return false;
            }

            // Return focus to target and paste
            TryRestoreForegroundWindow(targetHwnd);
            await Task.Delay(50);
            SendCtrlV();
            await Task.Delay(50);

            // Restore clipboard
            try
            {
                if (original != null) System.Windows.Clipboard.SetDataObject(original, true);
            }
            catch { }

            return true;
        }
    }
}
