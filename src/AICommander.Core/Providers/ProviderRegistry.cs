using System;
using System.Collections.Generic;
using System.Linq;
using AICommander.Core.Config;

namespace AICommander.Core.Providers;

/// <summary>
/// Registry for managing and querying available AI agent providers.
/// </summary>
public class ProviderRegistry
{
    private readonly List<IProvider> _providers = new();
    private AICommanderConfig? _config;

    /// <summary>
    /// Initializes the registry with the configuration and a set of providers.
    /// </summary>
    /// <param name="config">The application configuration.</param>
    /// <param name="providers">The collection of providers to register.</param>
    public void Initialize(AICommanderConfig config, IEnumerable<IProvider> providers)
    {
        _config = config;
        _providers.Clear();
        foreach (var provider in providers)
        {
            var configKey = config.Providers.Keys.FirstOrDefault(k => k.Equals(provider.Name, StringComparison.OrdinalIgnoreCase));
            if (configKey != null)
            {
                provider.Initialize(config.Providers[configKey]);
            }
            _providers.Add(provider);
        }
    }

    /// <summary>
    /// Gets the highest priority active provider that can handle the specified action.
    /// </summary>
    /// <param name="actionName">The name of the action.</param>
    /// <returns>The active provider, or null if none are found.</returns>
    public IProvider? GetActiveProviderForAction(string actionName)
    {
        if (_config?.ProviderPriority == null)
        {
            return null;
        }

        foreach (var providerName in _config.ProviderPriority)
        {
            var configKey = _config.Providers.Keys.FirstOrDefault(k => k.Equals(providerName, StringComparison.OrdinalIgnoreCase));
            if (configKey == null)
            {
                continue;
            }

            var providerConfig = _config.Providers[configKey];
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

    /// <summary>
    /// Gets a provider by its logical name.
    /// </summary>
    /// <param name="name">The name of the provider.</param>
    /// <returns>The provider, or null if not found.</returns>
    public IProvider? GetProviderByName(string name)
    {
        return _providers.FirstOrDefault(p => p.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
    }
}
