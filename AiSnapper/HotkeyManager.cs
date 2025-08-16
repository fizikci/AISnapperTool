using System;
using System.Runtime.InteropServices;
using System.Windows.Interop;
using System.Collections.Generic;

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
        C = 0x43,
        I = 0x49
    }

    public sealed class HotkeyManager : IDisposable
    {
        private readonly IntPtr _hwnd;
        private int _idCounter = 1;
        // Track callbacks per registered hotkey id
        private readonly Dictionary<int, Action> _callbacks = new();
        private HwndSource _source;

        public HotkeyManager(IntPtr hwnd)
        {
            _hwnd = hwnd;
            _source = HwndSource.FromHwnd(_hwnd)!;
            _source.AddHook(WndProc);
        }

        public void Register(Modifiers mods, VirtualKeys key, Action callback)
        {
            var id = _idCounter++;
            if (!RegisterHotKey(_hwnd, id, (uint)mods, (uint)key))
            {
                throw new InvalidOperationException("Failed to register hotkey. Try running as admin or change the combination.");
            }
            _callbacks[id] = callback;
        }

        public void Dispose()
        {
            // Unregister all hotkeys we registered
            foreach (var id in new List<int>(_callbacks.Keys))
            {
                UnregisterHotKey(_hwnd, id);
            }
            _callbacks.Clear();
            _source.RemoveHook(WndProc);
        }

        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            const int WM_HOTKEY = 0x0312;
            if (msg == WM_HOTKEY)
            {
                var id = wParam.ToInt32();
                if (_callbacks.TryGetValue(id, out var cb))
                {
                    cb?.Invoke();
                    handled = true;
                }
            }
            return IntPtr.Zero;
        }

        [DllImport("user32.dll")] private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);
        [DllImport("user32.dll")] private static extern bool UnregisterHotKey(IntPtr hWnd, int id);
    }
}
