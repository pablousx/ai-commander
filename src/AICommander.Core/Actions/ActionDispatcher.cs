using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using AICommander.Core.Config;
using AICommander.Core.Providers;

namespace AICommander.Core.Actions
{
    public class ActionDispatcher
    {
        private readonly ProviderRegistry _registry;
        private readonly AICommanderConfig _config;
        private readonly ILogger<ActionDispatcher> _logger;

        public ActionDispatcher(ProviderRegistry registry, AICommanderConfig config, ILogger<ActionDispatcher> logger)
        {
            _registry = registry ?? throw new ArgumentNullException(nameof(registry));
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task DispatchAsync(string actionId)
        {
            try
            {
                // actionId can be a global logical action like "accept" or provider specific like "vscode.accept"
                var parts = actionId.Split('.');
                
                IProvider activeProvider = null;
                string actionName = actionId;
                ProviderConfig providerConfig = null;

                if (parts.Length == 2)
                {
                    // Provider-specific action
                    var providerName = parts[0];
                    actionName = parts[1];
                    activeProvider = _registry.GetProviderByName(providerName);
                    
                    if (activeProvider != null && _config.Providers.TryGetValue(providerName, out providerConfig))
                    {
                        if (!activeProvider.IsRunning() || !activeProvider.IsVisible())
                        {
                            _logger.LogWarning($"Target provider '{providerName}' for action '{actionName}' is not active or visible.");
                            return;
                        }
                    }
                    else
                    {
                        _logger.LogWarning($"Provider '{providerName}' not found in configuration or registry.");
                        return;
                    }
                }
                else
                {
                    // Global logical action, resolve by priority
                    activeProvider = _registry.GetActiveProviderForAction(actionName);
                    if (activeProvider != null)
                    {
                        _config.Providers.TryGetValue(activeProvider.Name, out providerConfig);
                    }
                }

                if (activeProvider == null || providerConfig == null)
                {
                    _logger.LogInformation($"No active provider found to handle action '{actionName}'.");
                    return;
                }

                if (providerConfig.Actions.TryGetValue(actionName, out var actionConfig))
                {
                    _logger.LogInformation($"Executing action '{actionName}' on provider '{activeProvider.Name}'.");
                    await activeProvider.ExecuteAction(actionName, actionConfig);
                }
                else
                {
                    _logger.LogWarning($"Action '{actionName}' not configured for provider '{activeProvider.Name}'.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error dispatching action '{actionId}'");
            }
        }
    }
}
