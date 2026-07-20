using AICommander.Core.Config;
using AICommander.Core.Providers;

namespace AICommander.Tests.Fakes;

/// <summary>
/// Hand-written <see cref="IProvider"/> double for registry and dispatcher tests.
/// </summary>
public sealed class FakeProvider : IProvider
{
    private readonly Func<bool> _isRunning;
    private readonly Func<bool> _isVisible;

    public FakeProvider(
        string name,
        bool isRunning = true,
        bool isVisible = true,
        string? processName = null)
        : this(name, () => isRunning, () => isVisible, processName)
    {
    }

    public FakeProvider(
        string name,
        Func<bool> isRunning,
        Func<bool> isVisible,
        string? processName = null)
    {
        Name = name;
        ProcessName = processName ?? name.ToLowerInvariant();
        _isRunning = isRunning;
        _isVisible = isVisible;
    }

    public string Name { get; }

    public string ProcessName { get; private set; }

    public ProviderConfig? Config { get; private set; }

    public List<(string ActionName, ActionConfig Config)> ExecutedActions { get; } = new();

    public int ExecuteCallCount => ExecutedActions.Count;

    public void Initialize(ProviderConfig config)
    {
        Config = config;
        if (!string.IsNullOrEmpty(config.ProcessName))
        {
            ProcessName = config.ProcessName;
        }
    }

    public bool IsRunning() => _isRunning();

    public bool IsVisible() => _isVisible();

    public Task ExecuteAction(string actionName, ActionConfig actionConfig)
    {
        ExecutedActions.Add((actionName, actionConfig));
        return Task.CompletedTask;
    }
}
