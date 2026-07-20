using AICommander.Core.Config;
using AICommander.Core.Providers;
using AICommander.Tests.Fakes;

namespace AICommander.Tests;

public class ProviderRegistryTests
{
    [Fact]
    public void GetActiveProviderForAction_ReturnsHighestPriorityEnabledRunningVisible()
    {
        var config = CreateConfig(
            priority: ["antigravity", "vscode"],
            ("antigravity", Enabled: true, Actions: ["accept"]),
            ("vscode", Enabled: true, Actions: ["accept"]));

        var anti = new FakeProvider("antigravity", isRunning: true, isVisible: true);
        var vscode = new FakeProvider("vscode", isRunning: true, isVisible: true);

        var registry = CreateRegistry(config, anti, vscode);

        var active = registry.GetActiveProviderForAction("accept");

        Assert.Same(anti, active);
    }

    [Fact]
    public void GetActiveProviderForAction_SkipsDisabledProvider()
    {
        var config = CreateConfig(
            priority: ["antigravity", "vscode"],
            ("antigravity", Enabled: false, Actions: ["accept"]),
            ("vscode", Enabled: true, Actions: ["accept"]));

        var anti = new FakeProvider("antigravity");
        var vscode = new FakeProvider("vscode");
        var registry = CreateRegistry(config, anti, vscode);

        Assert.Same(vscode, registry.GetActiveProviderForAction("accept"));
    }

    [Fact]
    public void GetActiveProviderForAction_SkipsNotRunningProvider()
    {
        var config = CreateConfig(
            priority: ["antigravity", "vscode"],
            ("antigravity", Enabled: true, Actions: ["accept"]),
            ("vscode", Enabled: true, Actions: ["accept"]));

        var anti = new FakeProvider("antigravity", isRunning: false, isVisible: true);
        var vscode = new FakeProvider("vscode", isRunning: true, isVisible: true);
        var registry = CreateRegistry(config, anti, vscode);

        Assert.Same(vscode, registry.GetActiveProviderForAction("accept"));
    }

    [Fact]
    public void GetActiveProviderForAction_SkipsNotVisibleProvider()
    {
        var config = CreateConfig(
            priority: ["antigravity", "vscode"],
            ("antigravity", Enabled: true, Actions: ["accept"]),
            ("vscode", Enabled: true, Actions: ["accept"]));

        var anti = new FakeProvider("antigravity", isRunning: true, isVisible: false);
        var vscode = new FakeProvider("vscode", isRunning: true, isVisible: true);
        var registry = CreateRegistry(config, anti, vscode);

        Assert.Same(vscode, registry.GetActiveProviderForAction("accept"));
    }

    [Fact]
    public void GetActiveProviderForAction_SkipsProviderMissingAction()
    {
        var config = CreateConfig(
            priority: ["antigravity", "vscode"],
            ("antigravity", Enabled: true, Actions: ["reject"]),
            ("vscode", Enabled: true, Actions: ["accept"]));

        var anti = new FakeProvider("antigravity");
        var vscode = new FakeProvider("vscode");
        var registry = CreateRegistry(config, anti, vscode);

        Assert.Same(vscode, registry.GetActiveProviderForAction("accept"));
    }

    [Fact]
    public void GetActiveProviderForAction_ReturnsNull_WhenNoneQualify()
    {
        var config = CreateConfig(
            priority: ["antigravity"],
            ("antigravity", Enabled: true, Actions: ["accept"]));

        var anti = new FakeProvider("antigravity", isRunning: false, isVisible: false);
        var registry = CreateRegistry(config, anti);

        Assert.Null(registry.GetActiveProviderForAction("accept"));
    }

    [Fact]
    public void GetActiveProviderForAction_MatchesProviderNamesCaseInsensitively()
    {
        var config = CreateConfig(
            priority: ["AntiGravity"],
            ("antigravity", Enabled: true, Actions: ["accept"]));

        var anti = new FakeProvider("antigravity");
        var registry = CreateRegistry(config, anti);

        Assert.Same(anti, registry.GetActiveProviderForAction("accept"));
    }

    [Fact]
    public void GetProviderByName_ReturnsMatch_CaseInsensitive()
    {
        var config = CreateConfig(
            priority: ["vscode"],
            ("vscode", Enabled: true, Actions: ["accept"]));

        var vscode = new FakeProvider("vscode");
        var registry = CreateRegistry(config, vscode);

        Assert.Same(vscode, registry.GetProviderByName("VSCode"));
        Assert.Null(registry.GetProviderByName("missing"));
    }

    [Fact]
    public void Initialize_AppliesMatchingProviderConfig()
    {
        var config = CreateConfig(
            priority: ["vscode"],
            ("vscode", Enabled: true, Actions: ["accept"]));
        config.Providers["vscode"].ProcessName = "Code";

        var vscode = new FakeProvider("vscode");
        _ = CreateRegistry(config, vscode);

        Assert.NotNull(vscode.Config);
        Assert.Equal("Code", vscode.ProcessName);
    }

    private static ProviderRegistry CreateRegistry(AICommanderConfig config, params IProvider[] providers)
    {
        var registry = new ProviderRegistry();
        registry.Initialize(config, providers);
        return registry;
    }

    private static AICommanderConfig CreateConfig(
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
                providerConfig.Actions[action] = new ActionConfig
                {
                    KeySequence = ["y"]
                };
            }

            config.Providers[name] = providerConfig;
        }

        return config;
    }
}
