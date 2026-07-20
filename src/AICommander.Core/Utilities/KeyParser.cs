using System.Runtime.InteropServices;
using System.Windows.Input;

namespace AICommander.Core.Utilities;

/// <summary>
/// Utility class to parse key strings and modifiers into Virtual-Key (VK) codes.
/// </summary>
public static class KeyParser
{
    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern short VkKeyScan(char ch);

    // Aliases for keys that don't parse neatly via single char or WPF Key Enum
    private static readonly Dictionary<string, ushort> _keyAliasMap = new(StringComparer.OrdinalIgnoreCase)
    {
        { "backspace", 0x08 },
        { "return", 0x0D },
        { "ctrl", 0xA2 },
        { "control", 0xA2 },
        { "alt", 0xA4 },
        { "shift", 0xA0 },
        { "win", 0x5B },
        { "windows", 0x5B },
        { "arrowleft", 0x25 },
        { "arrowup", 0x26 },
        { "arrowright", 0x27 },
        { "arrowdown", 0x28 },
        // WPF Key enum uses OemOpenBrackets / OemCloseBrackets; accept friendly aliases too
        { "leftbracket", 0xDB },
        { "rightbracket", 0xDD },
        { "oemopenbrackets", 0xDB },
        { "oemclosebrackets", 0xDD }
    };

    /// <summary>
    /// Parses a key string to its corresponding Virtual-Key code.
    /// </summary>
    /// <param name="keyString">The string representation of the key (e.g., "Enter", "A", "LeftBracket").</param>
    /// <returns>The VK code, or 0 if the key is not recognized.</returns>
    public static ushort ParseKey(string? keyString)
    {
        if (string.IsNullOrWhiteSpace(keyString)) return 0;

        string lower = keyString.Trim().ToLowerInvariant();

        // 1. Check known aliases
        if (_keyAliasMap.TryGetValue(lower, out var vk))
        {
            return vk;
        }

        // 2. If it's a single character, use OS VkKeyScan
        if (keyString.Length == 1)
        {
            short scan = VkKeyScan(keyString[0]);
            if (scan != -1)
            {
                return (ushort)(scan & 0xFF);
            }
        }

        // 3. Try WPF Key Enum parsing for things like "F1", "Enter", "Space", "PageUp", etc.
        if (Enum.TryParse<Key>(keyString, true, out var wpfKey))
        {
            return (ushort)KeyInterop.VirtualKeyFromKey(wpfKey);
        }

        return 0;
    }

    /// <summary>
    /// Parses a string of modifiers into the format expected by RegisterHotKey.
    /// </summary>
    /// <param name="keys">A list of keys forming a hotkey (e.g., ["ctrl", "alt", "leftbracket"]).</param>
    /// <returns>A tuple containing the modifier bitmask and the primary Virtual-Key code.</returns>
    public static (uint Modifiers, uint Vk) ParseHotkey(IEnumerable<string> keys)
    {
        uint modifiers = 0;
        uint vk = 0;

        foreach (var key in keys)
        {
            var lower = key.Trim().ToLowerInvariant();
            switch (lower)
            {
                case "ctrl":
                case "control":
                    modifiers |= 0x0002;
                    break;
                case "alt":
                    modifiers |= 0x0001;
                    break;
                case "shift":
                    modifiers |= 0x0004;
                    break;
                case "win":
                case "windows":
                    modifiers |= 0x0008;
                    break;
                default:
                    vk = ParseKey(lower);
                    break;
            }
        }

        return (modifiers, vk);
    }
}
