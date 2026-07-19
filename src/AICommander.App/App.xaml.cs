using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using AICommander.Core.Config;
using AICommander.Core.HotkeyManager;
using AICommander.Core.Providers;
using AICommander.Core.Providers.Antigravity;
using AICommander.Core.Providers.VSCode;
using AICommander.Core.Providers.Claude;
using AICommander.Core.Actions;
using AICommander.Core.Utilities;
using System.Windows.Interop;
using Hardcodet.Wpf.TaskbarNotification;

namespace AICommander.App;

public partial class App : Application
{
    private ServiceProvider? _serviceProvider;
    private GlobalHotkeyManager? _hotkeyManager;
    private TaskbarIcon? _taskbarIcon;
    private MainWindow? _mainWindow;

    private void Application_Startup(object sender, StartupEventArgs e)
    {
        try
        {
            var services = new ServiceCollection();
            ConfigureServices(services);
            _serviceProvider = services.BuildServiceProvider();

            // Initialize MainWindow (creates HWND needed for HotkeyManager)
            _mainWindow = _serviceProvider.GetRequiredService<MainWindow>();
            var helper = new WindowInteropHelper(_mainWindow);
            helper.EnsureHandle();

            // Initialize Hotkey Manager
            _hotkeyManager = new GlobalHotkeyManager(helper.Handle, _serviceProvider.GetRequiredService<ILogger<GlobalHotkeyManager>>());
            
            RegisterHotkeys(_serviceProvider.GetRequiredService<AICommanderConfig>(), _serviceProvider.GetRequiredService<ActionDispatcher>());
            SetupTaskbarIcon();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Fatal error during startup: {ex}", "AI Commander Crash", MessageBoxButton.OK, MessageBoxImage.Error);
            Current.Shutdown();
        }
    }
    
    private void ConfigureServices(IServiceCollection services)
    {
        // Logging
        services.AddLogging(builder => 
        {
            builder.AddConsole();
        });

        // Config
        var configPath = GetConfigPath();
        var config = ConfigLoader.Load(configPath);
        services.AddSingleton(config);

        // Providers
        services.AddSingleton<IProvider, AntigravityProvider>();
        services.AddSingleton<IProvider, VSCodeProvider>();
        services.AddSingleton<IProvider, ClaudeProvider>();

        // Registry & Dispatcher
        services.AddSingleton<ProviderRegistry>(sp => 
        {
            var registry = new ProviderRegistry();
            registry.Initialize(sp.GetRequiredService<AICommanderConfig>(), sp.GetServices<IProvider>());
            return registry;
        });
        
        services.AddSingleton<ActionDispatcher>();

        // Windows
        services.AddSingleton<MainWindow>();
    }

    private static string GetConfigPath()
    {
        string baseDir = AppDomain.CurrentDomain.BaseDirectory;
        string configPath = Path.Combine(baseDir, "config", "ai-commander.yaml");

        DirectoryInfo? dir = new DirectoryInfo(baseDir);
        while (dir != null && !File.Exists(configPath))
        {
            configPath = Path.Combine(dir.FullName, "config", "ai-commander.yaml");
            dir = dir.Parent;
        }

        if (!File.Exists(configPath))
        {
            configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config", "ai-commander.yaml");
        }
        return configPath;
    }

    private void RegisterHotkeys(AICommanderConfig config, ActionDispatcher dispatcher)
    {
        foreach (var kvp in config.Hotkeys)
        {
            var keys = kvp.Key.Split('+');
            var (modifiers, vk) = KeyParser.ParseHotkey(keys);

            if (vk != 0 && _hotkeyManager != null)
            {
                var action = kvp.Value;
                _hotkeyManager.Register(modifiers, vk, async () =>
                {
                    // Visual feedback
                    if (_taskbarIcon != null)
                    {
                        // Fire-and-forget UI update
                        _ = Current.Dispatcher.InvokeAsync(() => 
                        {
                            _taskbarIcon.ShowBalloonTip("Action Triggered", $"Executing: {action}", BalloonIcon.Info);
                        });
                    }
                    
                    await dispatcher.DispatchAsync(action).ConfigureAwait(false);
                });
            }
        }
    }
    
    private void SetupTaskbarIcon()
    {
        _taskbarIcon = new TaskbarIcon
        {
            Icon = System.Drawing.SystemIcons.Application,
            ToolTipText = "AI Commander"
        };

        var menu = new System.Windows.Controls.ContextMenu();
        var exitItem = new System.Windows.Controls.MenuItem { Header = "Exit" };
        exitItem.Click += (s, ev) => Current.Shutdown();

        var settingsItem = new System.Windows.Controls.MenuItem { Header = "Settings" };
        settingsItem.Click += (s, ev) => _mainWindow?.Show();

        menu.Items.Add(settingsItem);
        menu.Items.Add(new System.Windows.Controls.Separator());
        menu.Items.Add(exitItem);

        _taskbarIcon.ContextMenu = menu;
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _hotkeyManager?.Dispose();
        _taskbarIcon?.Dispose();
        (_serviceProvider as IDisposable)?.Dispose();
        base.OnExit(e);
    }
}
