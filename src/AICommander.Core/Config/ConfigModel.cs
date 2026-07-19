using System.Collections.Generic;

namespace AICommander.Core.Config
{
    public class AICommanderConfig
    {
        public int Version { get; set; } = 1;
        public List<string> ProviderPriority { get; set; } = new List<string>();
        public Dictionary<string, ProviderConfig> Providers { get; set; } = new Dictionary<string, ProviderConfig>();
        public Dictionary<string, string> Hotkeys { get; set; } = new Dictionary<string, string>();
    }

    public class ProviderConfig
    {
        public bool Enabled { get; set; } = true;
        public string ProcessName { get; set; } = string.Empty;
        public Dictionary<string, ActionConfig> Actions { get; set; } = new Dictionary<string, ActionConfig>();
    }

    public class ActionConfig
    {
        public List<string> KeySequence { get; set; } = new List<string>();
    }
}
