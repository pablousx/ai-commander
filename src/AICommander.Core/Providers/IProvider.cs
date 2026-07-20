using AICommander.Core.Config;

namespace AICommander.Core.Providers;

/// <summary>
/// Interface representing an AI agent provider.
/// </summary>
public interface IProvider
{
    /// <summary>
    /// Gets the logical name of the provider.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Gets the process name of the target application.
    /// </summary>
    string ProcessName { get; }

    /// <summary>
    /// Initializes the provider with its configuration.
    /// </summary>
    /// <param name="config">The configuration for this provider.</param>
    void Initialize(ProviderConfig config);

    /// <summary>
    /// Checks if the target process is currently running.
    /// </summary>
    /// <returns>True if running; otherwise, false.</returns>
    bool IsRunning();

    /// <summary>
    /// Checks if the target process is currently visible.
    /// </summary>
    /// <returns>True if visible; otherwise, false.</returns>
    bool IsVisible();

    /// <summary>
    /// Executes an action by sending the provider's action keys
    /// to the target window. The keys are configured in the YAML file,
    /// NOT the hotkey that the user pressed.
    /// </summary>
    /// <param name="actionName">The name of the action to execute.</param>
    /// <param name="actionConfig">The configuration of the action.</param>
    Task ExecuteAction(string actionName, ActionConfig actionConfig);
}
