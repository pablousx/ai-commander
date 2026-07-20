using AICommander.Core.Actions;
using AICommander.Core.Config;
using AICommander.Core.Providers;
using AICommander.Tests.Fakes;
using Microsoft.Extensions.Logging.Abstractions;

namespace AICommander.Tests;

public class ActionDispatcherTests
{
    [Fact]
    public async Task DispatchAsync_GlobalAction_ExecutesOnActiveProvider()
    {
        var (dispatcher, vscode, acceptConfig) = CreateDispatcher(
            priority: ["vscode"],
            providers: [("vscode", Enabled: true, Running: true, Visible: true, Actions: ["accept"])]);

        await dispatcher.DispatchAsync("accept");

        Assert.Equal(1, vscode.ExecuteCallCount);
        Assert.Equal("accept", vscode.ExecutedActions[0].ActionName);
        Assert.Same(acceptConfig, vscode.ExecutedActions[0].Config);
    }

    [Fact]
    public async Task DispatchAsync_ProviderScopedAction_ExecutesWhenTargetActive()
    {
        var (dispatcher, vscode, acceptConfig) = CreateDispatcher(
            priority: ["antigravity", "vscode"],
            providers:
            [
                ("antigravity", Enabled: true, Running: true, Visible: true, Actions: ["accept"]),
                ("vscode", Enabled: true, Running: true, Visible: true, Actions: ["accept"])
            ]);

        await dispatcher.DispatchAsync("vscode.accept");

        Assert.Equal(1, vscode.ExecuteCallCount);
        Assert.Equal("accept", vscode.ExecutedActions[0].ActionName);
        Assert.Same(acceptConfig, vscode.ExecutedActions[0].Config);
    }

    [Fact]
    public async Task DispatchAsync_ProviderScopedAction_NoOpWhenTargetNotRunning()
    {
        var config = BuildConfig(
            priority: ["vscode"],
            ("vscode", Enabled: true, Actions: ["accept"]));

        var vscode = new FakeProvider("vscode", isRunning: false, isVisible: true);
        var registry = new ProviderRegistry();
        registry.Initialize(config, [vscode]);
        var dispatcher = new ActionDispatcher(registry, config, NullLogger<ActionDispatcher>.Instance);

        await dispatcher.DispatchAsync("vscode.accept");

        Assert.Equal(0, vscode.ExecuteCallCount);
    }

    [Fact]
    public async Task DispatchAsync_NoOp_WhenNoActiveProvider()
    {
        var config = BuildConfig(
            priority: ["vscode"],
            ("vscode", Enabled: true, Actions: ["accept"]));

        var vscode = new FakeProvider("vscode", isRunning: false, isVisible: false);
        var registry = new ProviderRegistry();
        registry.Initialize(config, [vscode]);
        var dispatcher = new ActionDispatcher(registry, config, NullLogger<ActionDispatcher>.Instance);

        await dispatcher.DispatchAsync("accept");

        Assert.Equal(0, vscode.ExecuteCallCount);
    }

    [Fact]
    public async Task DispatchAsync_NoOp_WhenActionMissingFromProviderConfig()
    {
        var config = BuildConfig(
            priority: ["vscode"],
            ("vscode", Enabled: true, Actions: ["reject"]));

        var vscode = new FakeProvider("vscode");
        var registry = new ProviderRegistry();
        registry.Initialize(config, [vscode]);
        var dispatcher = new ActionDispatcher(registry, config, NullLogger<ActionDispatcher>.Instance);

        await dispatcher.DispatchAsync("accept");

        Assert.Equal(0, vscode.ExecuteCallCount);
    }

    [Fact]
    public async Task DispatchAsync_ProviderScoped_NoOp_WhenProviderUnknown()
    {
        var config = BuildConfig(
            priority: ["vscode"],
            ("vscode", Enabled: true, Actions: ["accept"]));

        var vscode = new FakeProvider("vscode");
        var registry = new ProviderRegistry();
        registry.Initialize(config, [vscode]);
        var dispatcher = new ActionDispatcher(registry, config, NullLogger<ActionDispatcher>.Instance);

        await dispatcher.DispatchAsync("missing.accept");

        Assert.Equal(0, vscode.ExecuteCallCount);
    }

    private static (ActionDispatcher Dispatcher, FakeProvider Target, ActionConfig AcceptConfig) CreateDispatcher(
        string[] priority,
        (string Name, bool Enabled, bool Running, bool Visible, string[] Actions)[] providers)
    {
        var config = new AICommanderConfig
        {
            Version = 1,
            ProviderPriority = priority.ToList()
        };

        var fakes = new List<FakeProvider>();
        ActionConfig? acceptConfig = null;
        FakeProvider? target = null;

        foreach (var (name, enabled, running, visible, actions) in providers)
        {
            var providerConfig = new ProviderConfig
            {
                Enabled = enabled,
                ProcessName = name
            };
            foreach (var action in actions)
            {
                var actionConfig = new ActionConfig { KeySequence = ["y", "Enter"] };
                providerConfig.Actions[action] = actionConfig;
                if (action == "accept")
                {
                    acceptConfig = actionConfig;
                }
            }

            config.Providers[name] = providerConfig;
            var fake = new FakeProvider(name, running, visible);
            fakes.Add(fake);
            if (name.Equals("vscode", StringComparison.OrdinalIgnoreCase))
            {
                target = fake;
            }
        }

        target ??= fakes[0];
        acceptConfig ??= config.Providers[target.Name].Actions.Values.First();

        var registry = new ProviderRegistry();
        registry.Initialize(config, fakes.Cast<IProvider>());
        var dispatcher = new ActionDispatcher(registry, config, NullLogger<ActionDispatcher>.Instance);
        return (dispatcher, target, acceptConfig);
    }

    private static AICommanderConfig BuildConfig(
        string[] priority,
        params (string Name, bool Enabled, string[] Actions)[] providers)
    {
        var config = new AICommanderConfig
        {
            Version = 1,
            ProviderPriority = priority.ToList()
        };

        foreach (var (name, enabled, actions) in providers)
        {
            var providerConfig = new ProviderConfig
            {
                Enabled = enabled,
                ProcessName = name
            };
            foreach (var action in actions)
            {
                providerConfig.Actions[action] = new ActionConfig { KeySequence = ["y"] };
            }

            config.Providers[name] = providerConfig;
        }

        return config;
    }
}
