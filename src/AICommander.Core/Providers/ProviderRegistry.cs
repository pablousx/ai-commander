using System;
using System.Collections.Generic;
using System.Linq;
using AICommander.Core.Config;

namespace AICommander.Core.Providers
{
    public class ProviderRegistry
    {
        private readonly List<IProvider> _providers = new List<IProvider>();
        private AICommanderConfig _config;

        public void Initialize(AICommanderConfig config, IEnumerable<IProvider> providers)
        {
            _config = config;
            _providers.Clear();
            foreach (var provider in providers)
            {
                if (config.Providers.TryGetValue(provider.Name.ToLower(), out var providerConfig) || 
                    config.Providers.TryGetValue(provider.Name, out providerConfig))
                {
                    provider.Initialize(providerConfig);
                }
                _providers.Add(provider);
            }
        }

        public IProvider GetActiveProviderForAction(string actionName)
        {
            if (_config == null || _config.ProviderPriority == null)
            {
                return null;
            }

            foreach (var providerName in _config.ProviderPriority)
            {
                if (!_config.Providers.TryGetValue(providerName, out var providerConfig))
                {
                    continue;
                }

                if (!providerConfig.Enabled)
                {
                    continue;
                }

                if (!providerConfig.Actions.ContainsKey(actionName))
                {
                    continue;
                }

                var provider = _providers.FirstOrDefault(p => p.Name.Equals(providerName, StringComparison.OrdinalIgnoreCase));
                if (provider != null && provider.IsRunning() && provider.IsVisible())
                {
                    return provider;
                }
            }

            return null;
        }

        public IProvider GetProviderByName(string name)
        {
            return _providers.FirstOrDefault(p => p.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
        }
    }
}
