using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Windows.Input;
using System.Windows.Threading;
using AICommander.Core.Config;

namespace AICommander.App.ViewModels;

public class MainViewModel : ViewModelBase
{
    private readonly ConfigManager _configManager;
    private DispatcherTimer? _saveTimer;
    private bool _isLoading;
    private string _hintText = string.Empty;

    public string HintText
    {
        get => _hintText;
        set => SetProperty(ref _hintText, value);
    }

    public ObservableCollection<AppSectionViewModel> AppSections { get; } = new();

    public ICommand AddAppCommand { get; }
    public ICommand RemoveAppCommand { get; }
    public ICommand EditAppCommand { get; }

    public ObservableCollection<string> AvailableIcons { get; } = new ObservableCollection<string>
    {
        "pack://application:,,,/Assets/antigravity.png",
        "pack://application:,,,/Assets/vscode.png",
        "pack://application:,,,/Assets/claude.png"
    };

    public void RegisterIcon(string iconPath)
    {
        if (!string.IsNullOrWhiteSpace(iconPath) && !AvailableIcons.Contains(iconPath))
        {
            AvailableIcons.Add(iconPath);
        }
    }

    public static readonly System.Collections.Generic.Dictionary<string, System.Collections.Generic.List<string>> KnownTemplates = new()
    {
        { "Antigravity", new System.Collections.Generic.List<string> { "accept", "reject" } },
        { "VS Code", new System.Collections.Generic.List<string> { "accept", "inline_chat" } },
        { "Claude", new System.Collections.Generic.List<string> { "accept" } }
    };

    public MainViewModel(ConfigManager configManager)
    {
        _configManager = configManager;

        AppSections.CollectionChanged += OnAppSectionsCollectionChanged;
        LoadConfiguration();



        RemoveAppCommand = new RelayCommand<AppSectionViewModel>(item =>
        {
            if (item != null)
            {
                var result = System.Windows.MessageBox.Show(
                    $"Are you sure you want to delete the '{item.ProviderName}' app configuration?",
                    "Confirm Deletion",
                    System.Windows.MessageBoxButton.YesNo,
                    System.Windows.MessageBoxImage.Warning);

                if (result == System.Windows.MessageBoxResult.Yes)
                {
                    AppSections.Remove(item);
                }
            }
        });

        EditAppCommand = new RelayCommand<AppSectionViewModel>(item =>
        {
            if (item != null)
            {
                var editWindow = new AICommander.App.Views.AppConfigEditWindow(item, this);
                editWindow.Owner = App.Current.MainWindow;
                if (editWindow.ShowDialog() == true)
                {
                    // The view model properties are updated by the window if saved
                    RequestSave();
                }
            }
        });

        AddAppCommand = new RelayCommand<string>(template =>
        {
            if (string.IsNullOrWhiteSpace(template)) return;

            var newSection = new AppSectionViewModel(template, true, string.Empty, string.Empty);

            if (template.Equals("antigravity", StringComparison.OrdinalIgnoreCase))
            {
                newSection.ProcessName = "Antigravity";
                newSection.Icon = "pack://application:,,,/Assets/antigravity.png";
                newSection.Actions.Add(new AppActionViewModel("accept", "unset", "ctrl+enter"));
                newSection.Actions.Add(new AppActionViewModel("reject", "unset", "escape"));
            }
            else if (template.Equals("vscode", StringComparison.OrdinalIgnoreCase))
            {
                newSection.ProcessName = "Code"; // VS Code process is typically 'Code'
                newSection.Icon = "pack://application:,,,/Assets/vscode.png";
                newSection.Actions.Add(new AppActionViewModel("accept", "unset", "ctrl+enter"));
                newSection.Actions.Add(new AppActionViewModel("inline_chat", "unset", "ctrl+i"));
            }
            else if (template.Equals("claude", StringComparison.OrdinalIgnoreCase))
            {
                newSection.ProcessName = "Claude";
                newSection.Icon = "pack://application:,,,/Assets/claude.png";
                newSection.Actions.Add(new AppActionViewModel("accept", "unset", "ctrl+enter"));
            }
            else
            {
                // Custom app
                newSection.ProviderName = "Custom App";
                newSection.Icon = "pack://application:,,,/Assets/logo.ico";
                newSection.Actions.Add(new AppActionViewModel("new_action", "unset", "unset"));
            }

            AppSections.Add(newSection);
        });
    }

    private void LoadConfiguration()
    {
        _isLoading = true;
        AppSections.Clear();
        var config = _configManager.CurrentConfig;

        // Load based on priority first
        foreach (var providerName in config.ProviderPriority)
        {
            if (config.Providers.TryGetValue(providerName, out var providerConfig))
            {
                AddAppSection(providerName, providerConfig, config);
            }
        }

        // Add remaining
        foreach (var kvp in config.Providers)
        {
            if (!AppSections.Any(p => p.ProviderName.Equals(kvp.Key, StringComparison.OrdinalIgnoreCase)))
            {
                AddAppSection(kvp.Key, kvp.Value, config);
            }
        }
        UpdateDuplicateHotkeys();
        _isLoading = false;
    }

    private void AddAppSection(string providerName, ProviderConfig providerConfig, AICommanderConfig config)
    {
        string icon = providerConfig.Icon ?? string.Empty;
        RegisterIcon(icon);
        var section = new AppSectionViewModel(providerName, providerConfig.Enabled, providerConfig.ProcessName, icon);

        foreach (var actionKvp in providerConfig.Actions)
        {
            string actionName = actionKvp.Key;
            string appHotkey = string.Join("+", actionKvp.Value.KeySequence);

            // Find global hotkey mapping to this action
            string globalHotkey = "unset";
            var hotkeyMapping = config.Hotkeys.FirstOrDefault(x => x.Value.Equals(actionName, StringComparison.OrdinalIgnoreCase));
            if (hotkeyMapping.Key != null)
            {
                globalHotkey = hotkeyMapping.Key;
            }

            section.Actions.Add(new AppActionViewModel(actionName, globalHotkey, appHotkey));
        }

        AppSections.Add(section);
    }

    private void SaveConfiguration()
    {
        var config = _configManager.CurrentConfig;

        // Clear config to rebuild
        config.Hotkeys.Clear();
        config.ProviderPriority.Clear();

        var oldProviders = config.Providers.ToDictionary(k => k.Key, v => v.Value, StringComparer.OrdinalIgnoreCase);
        config.Providers.Clear();

        foreach (var section in AppSections)
        {
            config.ProviderPriority.Add(section.ProviderName);

            if (!oldProviders.TryGetValue(section.ProviderName, out var providerConfig))
            {
                providerConfig = new ProviderConfig();
            }

            providerConfig.Enabled = section.IsEnabled;
            providerConfig.ProcessName = section.ProcessName ?? string.Empty;
            providerConfig.Icon = section.Icon ?? string.Empty;

            // Rebuild actions
            providerConfig.Actions.Clear();
            foreach (var actionVm in section.Actions)
            {
                string finalActionName = actionVm.ActionName;
                if (string.IsNullOrWhiteSpace(finalActionName))
                {
                    finalActionName = $"Action {actionVm.Index}";
                }

                if (!string.IsNullOrWhiteSpace(actionVm.GlobalHotkey) && actionVm.GlobalHotkey != "unset")
                {
                    // Only register global hotkey if the section is enabled
                    if (section.IsEnabled)
                    {
                        config.Hotkeys[actionVm.GlobalHotkey] = finalActionName;
                    }
                }

                var keySeq = actionVm.AppHotkey.Split('+', StringSplitOptions.RemoveEmptyEntries).ToList();
                providerConfig.Actions[finalActionName] = new ActionConfig { KeySequence = keySeq };
            }

            config.Providers[section.ProviderName] = providerConfig;
        }

        _configManager.Save();

        // Apply hotkeys dynamically
        if (System.Windows.Application.Current is App app)
        {
            app.ReloadHotkeys();
        }
    }

    private void RequestSave()
    {
        if (_isLoading) return;

        if (_saveTimer == null)
        {
            _saveTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(500) };
            _saveTimer.Tick += (s, e) =>
            {
                _saveTimer.Stop();
                SaveConfiguration();
            };
        }
        _saveTimer.Stop();
        _saveTimer.Start();
    }

    private void OnAppSectionsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        RequestSave();
        if (e.NewItems != null)
        {
            foreach (AppSectionViewModel section in e.NewItems)
                SubscribeToSection(section);
        }
        if (e.OldItems != null)
        {
            foreach (AppSectionViewModel section in e.OldItems)
                UnsubscribeFromSection(section);
        }
    }

    private void SubscribeToSection(AppSectionViewModel section)
    {
        section.PropertyChanged += OnViewModelPropertyChanged;
        section.Actions.CollectionChanged += OnActionsCollectionChanged;
        foreach (var action in section.Actions)
            action.PropertyChanged += OnViewModelPropertyChanged;
    }

    private void UnsubscribeFromSection(AppSectionViewModel section)
    {
        section.PropertyChanged -= OnViewModelPropertyChanged;
        section.Actions.CollectionChanged -= OnActionsCollectionChanged;
        foreach (var action in section.Actions)
            action.PropertyChanged -= OnViewModelPropertyChanged;
    }

    private void OnActionsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        RequestSave();
        if (e.NewItems != null)
        {
            foreach (AppActionViewModel action in e.NewItems)
                action.PropertyChanged += OnViewModelPropertyChanged;
        }
        if (e.OldItems != null)
        {
            foreach (AppActionViewModel action in e.OldItems)
                action.PropertyChanged -= OnViewModelPropertyChanged;
        }
        UpdateDuplicateHotkeys();
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(AppActionViewModel.GlobalHotkey))
        {
            UpdateDuplicateHotkeys();
        }
        // Force refresh for system hotkeys when IsEnabled changes, since we drop disabled ones from registration
        if (e.PropertyName == nameof(AppSectionViewModel.IsEnabled))
        {
            UpdateDuplicateHotkeys();
        }
        RequestSave();
    }

    private void UpdateDuplicateHotkeys()
    {
        var allActions = AppSections.SelectMany(s => s.Actions).ToList();

        var duplicateKeys = allActions
            .Where(a => !string.IsNullOrWhiteSpace(a.GlobalHotkey) && a.GlobalHotkey != "unset")
            .GroupBy(a => a.GlobalHotkey, StringComparer.OrdinalIgnoreCase)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var action in allActions)
        {
            action.IsDuplicateGlobalHotkey = !string.IsNullOrWhiteSpace(action.GlobalHotkey) &&
                                             action.GlobalHotkey != "unset" &&
                                             duplicateKeys.Contains(action.GlobalHotkey);

            if (!string.IsNullOrWhiteSpace(action.GlobalHotkey) && action.GlobalHotkey != "unset")
            {
                var keys = action.GlobalHotkey.Split('+');
                var (modifiers, vk) = AICommander.Core.Utilities.KeyParser.ParseHotkey(keys);

                if (vk != 0)
                {
                    bool isAvailable = AICommander.Core.HotkeyManager.GlobalHotkeyManager.IsSystemHotkeyAvailable(modifiers, vk);
                    action.IsSystemGlobalHotkeyConflict = !isAvailable;
                }
                else
                {
                    action.IsSystemGlobalHotkeyConflict = false;
                }
            }
            else
            {
                action.IsSystemGlobalHotkeyConflict = false;
            }
        }
    }
}
