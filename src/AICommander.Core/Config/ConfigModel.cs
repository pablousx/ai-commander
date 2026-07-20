namespace AICommander.Core.Config;

public class AICommanderConfig
{
    public int Version { get; set; } = 1;
    public List<string> ProviderPriority { get; set; } = new();

    // Use case-insensitive dictionary for providers to avoid lookup bugs
    public Dictionary<string, ProviderConfig> Providers { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    // Use case-insensitive dictionary for hotkeys 
    public Dictionary<string, string> Hotkeys { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    public AppSettingsConfig Settings { get; set; } = new();
}

public class AppSettingsConfig
{
    public bool AutoStartOnBoot { get; set; } = false;
    public bool ShowTrayIcon { get; set; } = true;
}

public class ProviderConfig
{
    public bool Enabled { get; set; } = true;
    public string ProcessName { get; set; } = string.Empty;
    public string Icon { get; set; } = string.Empty;
    public Dictionary<string, ActionConfig> Actions { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}

public class ActionConfig
{
    public List<string> KeySequence { get; set; } = new();
}
