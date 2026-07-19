using System;
using System.Runtime.InteropServices;
using System.Windows.Interop;
using System.Collections.Generic;

namespace AICommander.Core.HotkeyManager
{
    public class GlobalHotkeyManager : IDisposable
    {
        [DllImport("user32.dll")]
        private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

        [DllImport("user32.dll")]
        private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        private const int WM_HOTKEY = 0x0312;

        private IntPtr _hWnd;
        private int _currentId = 0;
        private Dictionary<int, Action> _hotkeys = new Dictionary<int, Action>();

        public GlobalHotkeyManager(IntPtr hWnd)
        {
            _hWnd = hWnd;
            ComponentDispatcher.ThreadPreprocessMessage += ThreadPreprocessMessageMethod;
        }

        public void Register(uint modifiers, uint key, Action callback)
        {
            _currentId++;
            if (RegisterHotKey(_hWnd, _currentId, modifiers, key))
            {
                _hotkeys.Add(_currentId, callback);
            }
            else
            {
                // Fallo silencioso si la hotkey ya está ocupada
                System.Diagnostics.Debug.WriteLine($"Failed to register hotkey with modifiers {modifiers} and key {key}.");
            }
        }

        private void ThreadPreprocessMessageMethod(ref MSG msg, ref bool handled)
        {
            if (!handled && msg.message == WM_HOTKEY)
            {
                int id = msg.wParam.ToInt32();
                if (_hotkeys.TryGetValue(id, out var callback))
                {
                    callback.Invoke();
                    handled = true;
                }
            }
        }

        public void Dispose()
        {
            ComponentDispatcher.ThreadPreprocessMessage -= ThreadPreprocessMessageMethod;
            foreach (var id in _hotkeys.Keys)
            {
                UnregisterHotKey(_hWnd, id);
            }
            _hotkeys.Clear();
        }
    }
}
