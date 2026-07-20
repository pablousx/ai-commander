using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace AICommander.App.Views;

public class HotkeyCaptureControl : TextBox
{
    private bool _isRecording;
    private Brush? _originalBackground;
    private string? _originalText;

    private IntPtr _hookId = IntPtr.Zero;
    private LowLevelKeyboardProc? _hookCallback;
    private readonly HashSet<Key> _pressedKeys = new();

    private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool UnhookWindowsHookEx(IntPtr hhk);

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

    [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern IntPtr GetModuleHandle(string? lpModuleName);

    [StructLayout(LayoutKind.Sequential)]
    private struct KBDLLHOOKSTRUCT
    {
        public uint vkCode;
        public uint scanCode;
        public uint flags;
        public uint time;
        public IntPtr dwExtraInfo;
    }

    private const int WH_KEYBOARD_LL = 13;
    private const int WM_KEYDOWN = 0x0100;
    private const int WM_KEYUP = 0x0101;
    private const int WM_SYSKEYDOWN = 0x0104;
    private const int WM_SYSKEYUP = 0x0105;

    public HotkeyCaptureControl()
    {
        IsReadOnly = true;
        PreviewMouseLeftButtonDown += OnPreviewMouseLeftButtonDown;
        PreviewMouseRightButtonDown += OnPreviewMouseRightButtonDown;
        LostFocus += OnLostFocus;
        PreviewTextInput += (s, e) => e.Handled = true;
        Unloaded += (s, e) => StopRecording(revert: true);

        // Allow starting with Enter or Space
        PreviewKeyDown += (s, e) =>
        {
            if (!_isRecording && (e.Key == Key.Enter || e.Key == Key.Space))
            {
                StartRecording();
                e.Handled = true;
            }
        };
    }

    private void SetHint(string hint)
    {
        if (Window.GetWindow(this)?.DataContext is AICommander.App.ViewModels.MainViewModel vm)
        {
            vm.HintText = hint;
        }
    }

    private void OnPreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (!_isRecording)
        {
            StartRecording();
            Focus();
            e.Handled = true;
        }
    }

    private void OnPreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (!_isRecording)
        {
            SetCurrentValue(TextProperty, "unset");
            UpdateBinding();
            e.Handled = true;
        }
    }

    private void OnLostFocus(object sender, System.Windows.RoutedEventArgs e)
    {
        StopRecording(revert: true);
    }

    private void StartRecording()
    {
        if (_isRecording) return;
        _isRecording = true;
        _originalBackground = Background;
        _originalText = Text;

        Background = new SolidColorBrush(Color.FromArgb(30, 0, 120, 212));
        SetCurrentValue(TextProperty, "detecting keystrokes");
        SetHint("Type a keystroke... (Enter to accept, Esc to cancel)");

        _pressedKeys.Clear();
        _hookCallback = HookCallback;

        using var curProcess = Process.GetCurrentProcess();
        using var curModule = curProcess.MainModule;
        if (curModule != null)
        {
            _hookId = SetWindowsHookEx(WH_KEYBOARD_LL, _hookCallback, GetModuleHandle(curModule.ModuleName), 0);
        }
    }

    private void StopRecording(bool revert)
    {
        if (!_isRecording) return;
        _isRecording = false;

        if (_hookId != IntPtr.Zero)
        {
            UnhookWindowsHookEx(_hookId);
            _hookId = IntPtr.Zero;
        }

        SetHint(string.Empty);

        if (_originalBackground != null)
        {
            Background = _originalBackground;
        }

        if (revert && _originalText != null)
        {
            SetCurrentValue(TextProperty, _originalText);
        }
    }

    private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode >= 0)
        {
            int msg = wParam.ToInt32();
            if (msg == WM_KEYDOWN || msg == WM_SYSKEYDOWN || msg == WM_KEYUP || msg == WM_SYSKEYUP)
            {
                var kbd = Marshal.PtrToStructure<KBDLLHOOKSTRUCT>(lParam);
                var key = KeyInterop.KeyFromVirtualKey((int)kbd.vkCode);

                if (msg == WM_KEYDOWN || msg == WM_SYSKEYDOWN)
                {
                    if (HandleKeyDown(key))
                        return (IntPtr)1;
                }
                else
                {
                    if (HandleKeyUp(key))
                        return (IntPtr)1;
                }
            }
        }
        return CallNextHookEx(_hookId, nCode, wParam, lParam);
    }

    private bool HandleKeyDown(Key key)
    {
        if (key == Key.None) return true;

        _pressedKeys.Add(key);

        if (key == Key.Escape)
        {
            Dispatcher.InvokeAsync(() => StopRecording(revert: true));
            return true;
        }

        bool hasModifiers = _pressedKeys.Contains(Key.LeftCtrl) || _pressedKeys.Contains(Key.RightCtrl) ||
                            _pressedKeys.Contains(Key.LeftAlt) || _pressedKeys.Contains(Key.RightAlt) ||
                            _pressedKeys.Contains(Key.LeftShift) || _pressedKeys.Contains(Key.RightShift) ||
                            _pressedKeys.Contains(Key.LWin) || _pressedKeys.Contains(Key.RWin);

        if (key == Key.Enter && !hasModifiers)
        {
            Dispatcher.InvokeAsync(() =>
            {
                if (Text == "detecting keystrokes")
                {
                    StopRecording(revert: true);
                }
                else
                {
                    StopRecording(revert: false);
                    UpdateBinding();
                }
            });
            return true;
        }

        if (key == Key.Back || key == Key.Delete)
        {
            Dispatcher.InvokeAsync(() =>
            {
                SetCurrentValue(TextProperty, "unset");
                SetHint("Press Enter to save empty hotkey, Esc to cancel.");
            });
            return true;
        }

        if (IsModifier(key))
        {
            return true;
        }

        var parts = new List<string>();

        if (_pressedKeys.Contains(Key.LeftCtrl) || _pressedKeys.Contains(Key.RightCtrl)) parts.Add("Ctrl");
        if (_pressedKeys.Contains(Key.LeftAlt) || _pressedKeys.Contains(Key.RightAlt)) parts.Add("Alt");
        if (_pressedKeys.Contains(Key.LeftShift) || _pressedKeys.Contains(Key.RightShift)) parts.Add("Shift");
        if (_pressedKeys.Contains(Key.LWin) || _pressedKeys.Contains(Key.RWin)) parts.Add("Win");

        string keyStr = key.ToString();
        if (key >= Key.D0 && key <= Key.D9)
        {
            keyStr = key.ToString().Substring(1);
        }
        else if (key == Key.Return) keyStr = "Enter";
        else if (key == Key.OemPlus) keyStr = "+";
        else if (key == Key.OemMinus) keyStr = "-";
        else if (keyStr.StartsWith("NumPad")) keyStr = keyStr.Substring(6);

        parts.Add(keyStr);
        string result = string.Join("+", parts);

        Dispatcher.InvokeAsync(() =>
        {
            SetCurrentValue(TextProperty, result);
            SetHint("Press Enter to accept, Esc to cancel");
        });

        return true;
    }

    private bool HandleKeyUp(Key key)
    {
        _pressedKeys.Remove(key);
        return true;
    }

    private bool IsModifier(Key key)
    {
        return key == Key.LeftCtrl || key == Key.RightCtrl ||
               key == Key.LeftAlt || key == Key.RightAlt ||
               key == Key.LeftShift || key == Key.RightShift ||
               key == Key.LWin || key == Key.RWin ||
               key == Key.System;
    }

    private void UpdateBinding()
    {
        var binding = GetBindingExpression(TextProperty);
        binding?.UpdateSource();
    }
}
