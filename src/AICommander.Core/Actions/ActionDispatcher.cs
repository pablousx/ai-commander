using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using AICommander.Core.Config;
using AICommander.Core.Providers;

namespace AICommander.Core.Actions;

/// <summary>
/// Dispatches logical actions to the appropriate active provider.
/// </summary>
public class ActionDispatcher
{
    private readonly ProviderRegistry _registry;
    private readonly AICommanderConfig _config;
    private readonly ILogger<ActionDispatcher> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ActionDispatcher"/> class.
    /// </summary>
    /// <param name="registry">The provider registry.</param>
    /// <param name="config">The application configuration.</param>
    /// <param name="logger">The logger instance.</param>
    public ActionDispatcher(ProviderRegistry registry, AICommanderConfig config, ILogger<ActionDispatcher> logger)
    {
        _registry = registry ?? throw new ArgumentNullException(nameof(registry));
        _config = config ?? throw new ArgumentNullException(nameof(config));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Dispatches an action asynchronously.
    /// </summary>
    /// <param name="actionId">The ID of the action (e.g., "accept" or "vscode.accept").</param>
    public async Task DispatchAsync(string actionId)
    {
        try
        {
            var parts = actionId.Split('.');
            
            IProvider? activeProvider = null;
            string actionName = actionId;
            ProviderConfig? providerConfig = null;

            if (parts.Length == 2)
            {
                // Provider-specific action
                var providerName = parts[0];
                actionName = parts[1];
                activeProvider = _registry.GetProviderByName(providerName);
                
                if (activeProvider != null)
                {
                    // Case-insensitive lookup
                    var configKey = _config.Providers.Keys.FirstOrDefault(k => k.Equals(providerName, StringComparison.OrdinalIgnoreCase));
                    if (configKey != null)
                    {
                        providerConfig = _config.Providers[configKey];
                    }

                    if (providerConfig != null && (!activeProvider.IsRunning() || !activeProvider.IsVisible()))
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
                    var configKey = _config.Providers.Keys.FirstOrDefault(k => k.Equals(activeProvider.Name, StringComparison.OrdinalIgnoreCase));
                    if (configKey != null)
                    {
                        providerConfig = _config.Providers[configKey];
                    }
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
                await activeProvider.ExecuteAction(actionName, actionConfig).ConfigureAwait(false);
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
