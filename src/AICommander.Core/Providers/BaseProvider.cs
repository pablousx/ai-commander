using System;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using AICommander.Core.Config;

namespace AICommander.Core.Providers
{
    public abstract class BaseProvider : IProvider
    {
        protected ProviderConfig _config;

        public abstract string Name { get; }
        public virtual string ProcessName => _config?.ProcessName ?? Name.ToLower();

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

        public virtual bool IsRunning()
        {
            return Process.GetProcessesByName(ProcessName).Any();
        }

        public virtual bool IsVisible()
        {
            var process = Process.GetProcessesByName(ProcessName).FirstOrDefault(p => p.MainWindowHandle != IntPtr.Zero);
            return process != null;
        }

        public virtual async Task ExecuteAction(string actionName, ActionConfig actionConfig)
        {
            var process = Process.GetProcessesByName(ProcessName).FirstOrDefault(p => p.MainWindowHandle != IntPtr.Zero);
            if (process == null) return;

            IntPtr currentForeground = GetForegroundWindow();
            IntPtr targetWindow = process.MainWindowHandle;

            // Poner la ventana en foco
            SetForegroundWindow(targetWindow);
            
            // Breve pausa para asegurar que el sistema procesó el cambio de foco
            await Task.Delay(50);

            // Enviar secuencia de teclas
            SendKeySequence(actionConfig);

            // Breve pausa antes de restaurar el foco
            await Task.Delay(50);

            // Restaurar foco anterior
            if (currentForeground != IntPtr.Zero && currentForeground != targetWindow)
            {
                SetForegroundWindow(currentForeground);
            }
        }

        protected virtual void SendKeySequence(ActionConfig actionConfig)
        {
            foreach (var keyString in actionConfig.KeySequence)
            {
                // Un parser muy simple. Debería usar algo como WindowsForms SendKeys o un mapping a Virtual Keys (VK_TAB, etc)
                // Usaremos System.Windows.Forms.SendKeys en la app final, pero si estamos en Core sin WinForms
                // usaremos SendInput para teclas específicas o una librería de mapeo.
                
                // Para mantener la independencia de WinForms en Core, parseamos la tecla aquí
                ushort vk = ParseKey(keyString);
                
                var inputs = new INPUT[2];
                inputs[0] = new INPUT { type = INPUT_KEYBOARD };
                inputs[0].U.ki = new KEYBDINPUT { wVk = vk, dwFlags = 0 };

                inputs[1] = new INPUT { type = INPUT_KEYBOARD };
                inputs[1].U.ki = new KEYBDINPUT { wVk = vk, dwFlags = KEYEVENTF_KEYUP };

                SendInput(2, inputs, INPUT.Size);
                
                // Pausa entre teclas de la secuencia si es necesario
                Thread.Sleep(10);
            }
        }

        protected virtual ushort ParseKey(string keyString)
        {
            if (keyString.Equals("Enter", StringComparison.OrdinalIgnoreCase)) return 0x0D;
            if (keyString.Equals("Tab", StringComparison.OrdinalIgnoreCase)) return 0x09;
            if (keyString.Equals("Escape", StringComparison.OrdinalIgnoreCase)) return 0x1B;
            
            // Letras a-z
            if (keyString.Length == 1)
            {
                char c = char.ToUpper(keyString[0]);
                if (c >= 'A' && c <= 'Z')
                {
                    return (ushort)c;
                }
            }

            // Fallback (podría usar Enum.Parse de Keys si añadimos referencia)
            return 0;
        }
    }
}
