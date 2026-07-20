using System.Windows.Threading;
using AICommander.Core.Config;
using AICommander.Core.System;

namespace AICommander.App.ViewModels;

public class SettingsViewModel : ViewModelBase
{
    private readonly ConfigManager _configManager;
    private readonly SystemIntegrationService _systemIntegration;
    private DispatcherTimer? _saveTimer;

    private bool _autoStartOnBoot;
    private bool _showTrayIcon;

    public SettingsViewModel(ConfigManager configManager, SystemIntegrationService systemIntegration)
    {
        _configManager = configManager;
        _systemIntegration = systemIntegration;

        var settings = _configManager.CurrentConfig.Settings;
        _autoStartOnBoot = settings.AutoStartOnBoot;
        _showTrayIcon = settings.ShowTrayIcon;

        this.PropertyChanged += (s, e) => RequestSave();
    }

    public bool AutoStartOnBoot
    {
        get => _autoStartOnBoot;
        set => SetProperty(ref _autoStartOnBoot, value);
    }

    public bool ShowTrayIcon
    {
        get => _showTrayIcon;
        set => SetProperty(ref _showTrayIcon, value);
    }

    private void SaveSettings()
    {
        var settings = _configManager.CurrentConfig.Settings;

        settings.AutoStartOnBoot = AutoStartOnBoot;
        settings.ShowTrayIcon = ShowTrayIcon;

        _configManager.Save();

        // Apply system-level changes immediately
        _systemIntegration.SetAutoStart(AutoStartOnBoot);
    }

    private void RequestSave()
    {
        if (_saveTimer == null)
        {
            _saveTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(500) };
            _saveTimer.Tick += (s, e) =>
            {
                _saveTimer.Stop();
                SaveSettings();
            };
        }
        _saveTimer.Stop();
        _saveTimer.Start();
    }
}
