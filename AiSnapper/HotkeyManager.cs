using System;
using System.Runtime.InteropServices;
using System.Windows.Interop;

namespace AiSnapper
{
    [Flags]
    public enum Modifiers : uint
    {
        MOD_NONE = 0x0000,
        MOD_ALT = 0x0001,
        MOD_CONTROL = 0x0002,
        MOD_SHIFT = 0x0004,
        MOD_WIN = 0x0008
    }

    public enum VirtualKeys : uint
    {
        I = 0x49
    }

    public sealed class HotkeyManager : IDisposable
    {
        private readonly IntPtr _hwnd;
        private int _idCounter = 1;
        private int _currentId = 0;
        private Action? _callback;
        private HwndSource _source;

        public HotkeyManager(IntPtr hwnd)
        {
            _hwnd = hwnd;
            _source = HwndSource.FromHwnd(_hwnd)!;
            _source.AddHook(WndProc);
        }

        public void Register(Modifiers mods, VirtualKeys key, Action callback)
        {
            _callback = callback;
            _currentId = _idCounter++;
            if (!RegisterHotKey(_hwnd, _currentId, (uint)mods, (uint)key))
            {
                throw new InvalidOperationException("Failed to register hotkey. Try running as admin or change the combination.");
            }
        }

        public void Dispose()
        {
            if (_currentId != 0) UnregisterHotKey(_hwnd, _currentId);
            _source.RemoveHook(WndProc);
        }

        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            const int WM_HOTKEY = 0x0312;
            if (msg == WM_HOTKEY && wParam.ToInt32() == _currentId)
            {
                _callback?.Invoke();
                handled = true;
            }
            return IntPtr.Zero;
        }

        [DllImport("user32.dll")] private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);
        [DllImport("user32.dll")] private static extern bool UnregisterHotKey(IntPtr hWnd, int id);
    }
}
