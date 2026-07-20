using System.Diagnostics;
using System.Runtime.InteropServices;
using AICommander.Core.Config;
using AICommander.Core.Utilities;

namespace AICommander.Core.Providers;

/// <summary>
/// Base class for all AI agent providers, handling window focus and key sequences.
/// </summary>
public abstract class BaseProvider : IProvider
{
    protected ProviderConfig? _config;

    /// <summary>
    /// Gets the logical name of the provider.
    /// </summary>
    public abstract string Name { get; }

    /// <summary>
    /// Gets the process name of the target application.
    /// </summary>
    public virtual string ProcessName => _config?.ProcessName ?? Name.ToLowerInvariant();

    /// <summary>
    /// Initializes the provider with its configuration.
    /// </summary>
    /// <param name="config">The configuration for this provider.</param>
    public virtual void Initialize(ProviderConfig config)
    {
        _config = config;
    }

    [DllImport("user32.dll")]
    protected static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    protected static extern bool SetForegroundWindow(IntPtr hWnd);

    [DllImport("user32.dll", SetLastError = true)]
    protected static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);

    [StructLayout(LayoutKind.Sequential)]
    protected struct INPUT
    {
        public uint type;
        public InputUnion U;
        public static int Size => Marshal.SizeOf(typeof(INPUT));
    }

    [StructLayout(LayoutKind.Explicit)]
    protected struct InputUnion
    {
        [FieldOffset(0)]
        public MOUSEINPUT mi;
        [FieldOffset(0)]
        public KEYBDINPUT ki;
        [FieldOffset(0)]
        public HARDWAREINPUT hi;
    }

    [StructLayout(LayoutKind.Sequential)]
    protected struct MOUSEINPUT
    {
        public int dx;
        public int dy;
        public uint mouseData;
        public uint dwFlags;
        public uint time;
        public IntPtr dwExtraInfo;
    }

    [StructLayout(LayoutKind.Sequential)]
    protected struct HARDWAREINPUT
    {
        public uint uMsg;
        public ushort wParamL;
        public ushort wParamH;
    }

    [StructLayout(LayoutKind.Sequential)]
    protected struct KEYBDINPUT
    {
        public ushort wVk;
        public ushort wScan;
        public uint dwFlags;
        public uint time;
        public IntPtr dwExtraInfo;
    }

    protected const uint INPUT_KEYBOARD = 1;
    protected const uint KEYEVENTF_EXTENDEDKEY = 0x0001;
    protected const uint KEYEVENTF_KEYUP = 0x0002;

    [DllImport("user32.dll")]
    protected static extern uint MapVirtualKey(uint uCode, uint uMapType);

    /// <summary>
    /// Checks if the target process is currently running.
    /// </summary>
    /// <returns>True if running; otherwise, false.</returns>
    public virtual bool IsRunning()
    {
        return GetMainProcess() != null;
    }

    /// <summary>
    /// Checks if the target process is currently visible.
    /// </summary>
    /// <returns>True if visible; otherwise, false.</returns>
    public virtual bool IsVisible()
    {
        var process = GetMainProcess();
        return process != null && process.MainWindowHandle != IntPtr.Zero;
    }

    /// <summary>
    /// Gets the main process associated with this provider.
    /// </summary>
    /// <returns>The <see cref="Process"/> instance or null if not found.</returns>
    protected virtual Process? GetMainProcess()
    {
        string pName = ProcessName;
        if (pName.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
        {
            pName = pName.Substring(0, pName.Length - 4);
        }
        return Process.GetProcessesByName(pName).FirstOrDefault();
    }

    /// <summary>
    /// Executes an action by sending the configured key sequence to the target window.
    /// </summary>
    /// <param name="actionName">The name of the action being executed.</param>
    /// <param name="actionConfig">The configuration containing the key sequence.</param>
    public virtual async Task ExecuteAction(string actionName, ActionConfig actionConfig)
    {
        var process = GetMainProcess();
        if (process == null || process.MainWindowHandle == IntPtr.Zero) return;

        IntPtr currentForeground = GetForegroundWindow();
        IntPtr targetWindow = process.MainWindowHandle;

        // Bring the target window to the foreground
        SetForegroundWindow(targetWindow);

        // Release physical modifiers to prevent them from bleeding into the simulated sequence
        ushort[] physicalModifiers = { 0x10, 0x11, 0x12, 0x5B, 0x5C, 0xA0, 0xA1, 0xA2, 0xA3, 0xA4, 0xA5 };
        var releaseInputs = new global::System.Collections.Generic.List<INPUT>();
        foreach (var mod in physicalModifiers)
        {
            var input = new INPUT { type = INPUT_KEYBOARD };
            input.U.ki = new KEYBDINPUT { wVk = mod, wScan = (ushort)MapVirtualKey(mod, 0), dwFlags = KEYEVENTF_KEYUP };
            releaseInputs.Add(input);
        }
        SendInput((uint)releaseInputs.Count, releaseInputs.ToArray(), INPUT.Size);

        // Generous pause to ensure the system processes the focus change and target app is ready
        await Task.Delay(200).ConfigureAwait(false);

        // Send the key sequence
        await SendKeySequenceAsync(actionConfig).ConfigureAwait(false);

        // Brief pause before restoring focus
        await Task.Delay(50).ConfigureAwait(false);

        // Restore previous focus
        if (currentForeground != IntPtr.Zero && currentForeground != targetWindow)
        {
            SetForegroundWindow(currentForeground);
        }
    }

    /// <summary>
    /// Sends a sequence of keys to the currently focused window.
    /// </summary>
    /// <param name="actionConfig">The configuration containing the key sequence.</param>
    protected virtual async Task SendKeySequenceAsync(ActionConfig actionConfig)
    {
        var pressedModifiers = new global::System.Collections.Generic.List<ushort>();

        foreach (var keyString in actionConfig.KeySequence)
        {
            ushort vk = KeyParser.ParseKey(keyString);

            if (vk == 0) continue; // Skip unmapped keys (e.g., long command strings)

            bool isModifier =
                vk == 0x11 || vk == 0xA2 || vk == 0xA3 || // Ctrl, LCtrl, RCtrl
                vk == 0x12 || vk == 0xA4 || vk == 0xA5 || // Alt, LAlt, RAlt
                vk == 0x10 || vk == 0xA0 || vk == 0xA1 || // Shift, LShift, RShift
                vk == 0x5B || vk == 0x5C;                 // LWin, RWin

            ushort scanCode = (ushort)MapVirtualKey(vk, 0); // MAPVK_VK_TO_VSC
            uint flags = 0;

            // Apply Extended Key flag for right modifiers and arrow keys
            if (vk == 0xA3 || vk == 0xA5 || vk == 0x5B || vk == 0x5C ||
                (vk >= 0x21 && vk <= 0x28) || vk == 0x2D || vk == 0x2E)
            {
                flags |= KEYEVENTF_EXTENDEDKEY;
            }

            if (isModifier)
            {
                var input = new INPUT { type = INPUT_KEYBOARD };
                input.U.ki = new KEYBDINPUT { wVk = vk, wScan = scanCode, dwFlags = flags };
                SendInput(1, new[] { input }, INPUT.Size);
                pressedModifiers.Add(vk);
            }
            else
            {
                var inputs = new INPUT[2];
                inputs[0] = new INPUT { type = INPUT_KEYBOARD };
                inputs[0].U.ki = new KEYBDINPUT { wVk = vk, wScan = scanCode, dwFlags = flags };

                inputs[1] = new INPUT { type = INPUT_KEYBOARD };
                inputs[1].U.ki = new KEYBDINPUT { wVk = vk, wScan = scanCode, dwFlags = flags | KEYEVENTF_KEYUP };

                SendInput(2, inputs, INPUT.Size);
            }

            // Pause between keys in the sequence
            await Task.Delay(10).ConfigureAwait(false);
        }

        // Release modifiers in reverse order
        pressedModifiers.Reverse();
        foreach (var vk in pressedModifiers)
        {
            ushort scanCode = (ushort)MapVirtualKey(vk, 0);
            uint flags = KEYEVENTF_KEYUP;

            if (vk == 0xA3 || vk == 0xA5 || vk == 0x5B || vk == 0x5C ||
                (vk >= 0x21 && vk <= 0x28) || vk == 0x2D || vk == 0x2E)
            {
                flags |= KEYEVENTF_EXTENDEDKEY;
            }

            var input = new INPUT { type = INPUT_KEYBOARD };
            input.U.ki = new KEYBDINPUT { wVk = vk, wScan = scanCode, dwFlags = flags };
            SendInput(1, new[] { input }, INPUT.Size);
            await Task.Delay(10).ConfigureAwait(false);
        }
    }
}
