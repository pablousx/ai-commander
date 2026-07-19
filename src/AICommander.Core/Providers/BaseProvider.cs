using System;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
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
        public KEYBDINPUT ki;
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
    protected const uint KEYEVENTF_KEYUP = 0x0002;

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
        return Process.GetProcessesByName(ProcessName).FirstOrDefault();
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
        
        // Brief pause to ensure the system processed the focus change
        await Task.Delay(50).ConfigureAwait(false);

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
        foreach (var keyString in actionConfig.KeySequence)
        {
            ushort vk = KeyParser.ParseKey(keyString);
            
            if (vk == 0) continue; // Skip unmapped keys (e.g., long command strings)
            
            var inputs = new INPUT[2];
            inputs[0] = new INPUT { type = INPUT_KEYBOARD };
            inputs[0].U.ki = new KEYBDINPUT { wVk = vk, dwFlags = 0 };

            inputs[1] = new INPUT { type = INPUT_KEYBOARD };
            inputs[1].U.ki = new KEYBDINPUT { wVk = vk, dwFlags = KEYEVENTF_KEYUP };

            SendInput(2, inputs, INPUT.Size);
            
            // Pause between keys in the sequence
            await Task.Delay(10).ConfigureAwait(false);
        }
    }
}
