using System;
using System.Collections.Generic;

namespace AICommander.Core.Utilities;

/// <summary>
/// Utility class to parse key strings and modifiers into Virtual-Key (VK) codes.
/// </summary>
public static class KeyParser
{
    private static readonly Dictionary<string, ushort> _keyMap = new(StringComparer.OrdinalIgnoreCase)
    {
        { "backspace", 0x08 },
        { "tab", 0x09 },
        { "enter", 0x0D },
        { "return", 0x0D },
        { "escape", 0x1B },
        { "space", 0x20 },
        { "pageup", 0x21 },
        { "pagedown", 0x22 },
        { "end", 0x23 },
        { "home", 0x24 },
        { "left", 0x25 },
        { "arrowleft", 0x25 },
        { "up", 0x26 },
        { "arrowup", 0x26 },
        { "right", 0x27 },
        { "arrowright", 0x27 },
        { "down", 0x28 },
        { "arrowdown", 0x28 },
        { "delete", 0x2E },
        { "leftbracket", 0xDB },
        { "[", 0xDB },
        { "backslash", 0xDC },
        { "\\", 0xDC },
        { "rightbracket", 0xDD },
        { "]", 0xDD }
    };

    static KeyParser()
    {
        // Add A-Z
        for (char c = 'A'; c <= 'Z'; c++)
        {
            _keyMap.Add(c.ToString(), (ushort)c);
        }
        
        // Add 0-9
        for (char c = '0'; c <= '9'; c++)
        {
            _keyMap.Add(c.ToString(), (ushort)c);
        }

        // Add F1-F24
        for (int i = 1; i <= 24; i++)
        {
            _keyMap.Add($"F{i}", (ushort)(0x70 + i - 1));
        }
    }

    /// <summary>
    /// Parses a key string to its corresponding Virtual-Key code.
    /// </summary>
    /// <param name="keyString">The string representation of the key (e.g., "Enter", "A", "LeftBracket").</param>
    /// <returns>The VK code, or 0 if the key is not recognized.</returns>
    public static ushort ParseKey(string keyString)
    {
        if (string.IsNullOrWhiteSpace(keyString)) return 0;
        
        if (_keyMap.TryGetValue(keyString, out var vk))
        {
            return vk;
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
