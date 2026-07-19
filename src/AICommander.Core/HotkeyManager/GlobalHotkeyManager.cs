using System;
using System.Runtime.InteropServices;
using System.Windows.Interop;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;

namespace AICommander.Core.HotkeyManager;

/// <summary>
/// Manages registration and routing of global hotkeys.
/// </summary>
public class GlobalHotkeyManager : IDisposable
{
    [DllImport("user32.dll")]
    private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

    [DllImport("user32.dll")]
    private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

    private const int WM_HOTKEY = 0x0312;

    private readonly IntPtr _hWnd;
    private readonly ILogger<GlobalHotkeyManager> _logger;
    private int _currentId = 0;
    private readonly Dictionary<int, Action> _hotkeys = new();

    public GlobalHotkeyManager(IntPtr hWnd, ILogger<GlobalHotkeyManager> logger)
    {
        _hWnd = hWnd;
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        ComponentDispatcher.ThreadPreprocessMessage += ThreadPreprocessMessageMethod;
    }

    /// <summary>
    /// Registers a global hotkey with the OS.
    /// </summary>
    public void Register(uint modifiers, uint key, Action callback)
    {
        _currentId++;
        if (RegisterHotKey(_hWnd, _currentId, modifiers, key))
        {
            _hotkeys.Add(_currentId, callback);
            _logger.LogInformation($"Successfully registered hotkey (Modifiers: {modifiers}, Key: {key})");
        }
        else
        {
            _logger.LogWarning($"Failed to register hotkey (Modifiers: {modifiers}, Key: {key}). It may be in use by another application.");
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
