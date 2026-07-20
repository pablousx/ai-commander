namespace AICommander.Core.Config;

public class ConfigManager
{
    private readonly string _configPath;

    public AICommanderConfig CurrentConfig { get; private set; }

    public ConfigManager(string configPath)
    {
        _configPath = configPath;
        CurrentConfig = ConfigLoader.Load(_configPath);
    }

    public void Save()
    {
        ConfigLoader.Save(CurrentConfig, _configPath);
    }

    public void Reload()
    {
        CurrentConfig = ConfigLoader.Load(_configPath);
    }
}
