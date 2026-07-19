using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using Microsoft.Extensions.Logging;
using AICommander.Core.Config;
using AICommander.Core.HotkeyManager;
using AICommander.Core.Providers;
using AICommander.Core.Providers.Antigravity;
using AICommander.Core.Providers.VSCode;
using AICommander.Core.Providers.Claude;
using AICommander.Core.Actions;
using System.Windows.Interop;
using Hardcodet.Wpf.TaskbarNotification;

namespace AICommander.App
{
    public partial class App : Application
    {
        private GlobalHotkeyManager _hotkeyManager;
        private ProviderRegistry _providerRegistry;
        private ActionDispatcher _actionDispatcher;
        private AICommanderConfig _config;
        private ILogger<ActionDispatcher> _logger;
        private TaskbarIcon _taskbarIcon;
        private MainWindow _mainWindow;

        private void Application_Startup(object sender, StartupEventArgs e)
        {
            File.AppendAllText("startup_log.txt", "1. Entered Application_Startup\n");
            try
            {
                File.AppendAllText("startup_log.txt", "2. Creating LoggerFactory\n");
                // Setup minimal logging
                using var loggerFactory = LoggerFactory.Create(builder =>
                {
                    builder.AddConsole();
                });
                _logger = loggerFactory.CreateLogger<ActionDispatcher>();

                File.AppendAllText("startup_log.txt", "3. Loading config\n");
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            string configPath = Path.Combine(baseDir, "config", "ai-commander.yaml");

            // Si estamos en entorno de desarrollo (ej. bin/Debug/net8.0-windows), subir en los directorios
            DirectoryInfo dir = new DirectoryInfo(baseDir);
            while (dir != null && !File.Exists(configPath))
            {
                configPath = Path.Combine(dir.FullName, "config", "ai-commander.yaml");
                dir = dir.Parent;
            }

            // Fallback final
            if (!File.Exists(configPath))
            {
                configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config", "ai-commander.yaml");
            }

            try
            {
                _config = ConfigLoader.Load(configPath);
                File.AppendAllText("startup_log.txt", "4. Config loaded successfully\n");
            }
            catch (Exception ex)
            {
                File.AppendAllText("startup_log.txt", $"Config Load Error: {ex.Message}\n");
                MessageBox.Show($"Failed to load configuration: {ex.Message}", "AI Commander", MessageBoxButton.OK, MessageBoxImage.Error);
                Current.Shutdown();
                return;
            }

            File.AppendAllText("startup_log.txt", "5. Initializing Provider Registry\n");
            _providerRegistry = new ProviderRegistry();
            var providers = new List<IProvider>
            {
                new AntigravityProvider(),
                new VSCodeProvider(),
                new ClaudeProvider()
            };
            _providerRegistry.Initialize(_config, providers);

            // Init Dispatcher
            _actionDispatcher = new ActionDispatcher(_providerRegistry, _config, _logger);

            // Init Hotkey Manager using a dummy window handle since WPF App doesn't have one initially
            _mainWindow = new MainWindow();
            var helper = new WindowInteropHelper(_mainWindow);
            helper.EnsureHandle(); // creates handle without showing window

            _hotkeyManager = new GlobalHotkeyManager(helper.Handle);
            
            File.AppendAllText("startup_log.txt", "6. Registering hotkeys\n");
            RegisterHotkeys();

            File.AppendAllText("startup_log.txt", "7. Initializing TaskbarIcon\n");
            // Init Taskbar Icon programmatically for now
            _taskbarIcon = new TaskbarIcon
            {
                Icon = System.Drawing.SystemIcons.Application,
                ToolTipText = "AI Commander"
            };

            // Simple Context Menu
            var menu = new System.Windows.Controls.ContextMenu();
            var exitItem = new System.Windows.Controls.MenuItem { Header = "Exit" };
            exitItem.Click += (s, ev) => Current.Shutdown();

            var settingsItem = new System.Windows.Controls.MenuItem { Header = "Settings" };
            settingsItem.Click += (s, ev) => _mainWindow.Show();

            menu.Items.Add(settingsItem);
            menu.Items.Add(new System.Windows.Controls.Separator());
            menu.Items.Add(exitItem);

            _taskbarIcon.ContextMenu = menu;
            File.AppendAllText("startup_log.txt", "8. Setup complete, exiting Application_Startup\n");
            }
            catch (Exception ex)
            {
                File.AppendAllText("startup_log.txt", $"FATAL EXCEPTION: {ex.ToString()}\n");
                MessageBox.Show($"Fatal error during startup: {ex.ToString()}", "AI Commander Crash", MessageBoxButton.OK, MessageBoxImage.Error);
                Current.Shutdown();
            }
        }

        private void RegisterHotkeys()
        {
            // Simple mapping from config
            foreach (var kvp in _config.Hotkeys)
            {
                var keys = kvp.Key.Split('+').Select(k => k.Trim().ToLower());
                uint modifiers = 0;
                uint vk = 0;

                foreach (var key in keys)
                {
                    if (key == "ctrl") modifiers |= 0x0002;
                    else if (key == "alt") modifiers |= 0x0001;
                    else if (key == "shift") modifiers |= 0x0004;
                    else if (key == "win") modifiers |= 0x0008;
                    else
                    {
                        // Parse simple key
                        if (key.Length == 1)
                        {
                            vk = (uint)char.ToUpper(key[0]);
                        }
                        else if (key == "escape") vk = 0x1B;
                        // More key parsing needed for production
                    }
                }

                if (vk != 0)
                {
                    var action = kvp.Value;
                    _hotkeyManager.Register(modifiers, vk, async () =>
                    {
                        await _actionDispatcher.DispatchAsync(action);
                    });
                }
            }
        }

        protected override void OnExit(ExitEventArgs e)
        {
            _hotkeyManager?.Dispose();
            _taskbarIcon?.Dispose();
            base.OnExit(e);
        }
    }
}
