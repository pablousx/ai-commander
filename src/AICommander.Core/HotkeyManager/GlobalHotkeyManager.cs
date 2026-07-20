using System.Runtime.InteropServices;
using System.Windows.Interop;
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
    private static readonly HashSet<(uint Modifiers, uint Key)> _ourRegisteredHotkeys = new();

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
            _ourRegisteredHotkeys.Add((modifiers, key));
            _logger.LogInformation($"Successfully registered hotkey (Modifiers: {modifiers}, Key: {key})");
        }
        else
        {
            _logger.LogWarning($"Failed to register hotkey (Modifiers: {modifiers}, Key: {key}). It may be in use by another application.");
        }
    }

    public static bool IsSystemHotkeyAvailable(uint modifiers, uint key)
    {
        if (_ourRegisteredHotkeys.Contains((modifiers, key)))
        {
            return true; // We own it, so it's not a system conflict
        }

        bool registered = RegisterHotKey(IntPtr.Zero, 0x1337, modifiers, key);
        if (registered)
        {
            UnregisterHotKey(IntPtr.Zero, 0x1337);
            return true;
        }
        return false;
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

    public void ClearHotkeys()
    {
        foreach (var id in _hotkeys.Keys)
        {
            UnregisterHotKey(_hWnd, id);
        }
        _hotkeys.Clear();
        _ourRegisteredHotkeys.Clear();
    }

    public void Dispose()
    {
        ComponentDispatcher.ThreadPreprocessMessage -= ThreadPreprocessMessageMethod;
        ClearHotkeys();
    }
}
