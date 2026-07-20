using AICommander.Core.Config;

namespace AICommander.Tests;

public class ConfigLoaderTests
{
    [Fact]
    public void Load_ValidConfig_ReturnsDeserializedConfig()
    {
        var tempFile = Path.GetTempFileName();
        var yaml = """
            version: 1
            provider_priority:
              - antigravity
            providers:
              antigravity:
                enabled: true
                process_name: antigravity
                actions:
                  accept:
                    key_sequence: ["y", "Enter"]
            hotkeys:
              "ctrl+alt+win+o": "accept"
            """;
        File.WriteAllText(tempFile, yaml);

        try
        {
            var config = ConfigLoader.Load(tempFile);

            Assert.NotNull(config);
            Assert.Equal(1, config.Version);
            Assert.Single(config.ProviderPriority);
            Assert.Equal("antigravity", config.ProviderPriority[0]);
            Assert.True(config.Providers.ContainsKey("antigravity"));

            var provider = config.Providers["antigravity"];
            Assert.True(provider.Enabled);
            Assert.Equal("antigravity", provider.ProcessName);
            Assert.True(provider.Actions.ContainsKey("accept"));

            var action = provider.Actions["accept"];
            Assert.Equal(2, action.KeySequence.Count);
            Assert.Equal("y", action.KeySequence[0]);
            Assert.Equal("Enter", action.KeySequence[1]);

            Assert.True(config.Hotkeys.ContainsKey("ctrl+alt+win+o"));
            Assert.Equal("accept", config.Hotkeys["ctrl+alt+win+o"]);
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public void Load_FileNotFound_ThrowsFileNotFoundException()
    {
        Assert.Throws<FileNotFoundException>(() => ConfigLoader.Load("non_existent_file.yaml"));
    }

    [Fact]
    public void Load_EmptyFile_ThrowsInvalidDataException()
    {
        var tempFile = Path.GetTempFileName();
        File.WriteAllText(tempFile, string.Empty);

        try
        {
            Assert.Throws<InvalidDataException>(() => ConfigLoader.Load(tempFile));
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public void Save_ThenLoad_PreservesHotkeysAndActions()
    {
        var tempFile = Path.Combine(Path.GetTempPath(), $"ai-commander-test-{Guid.NewGuid():N}.yaml");
        var original = new AICommanderConfig
        {
            Version = 1,
            ProviderPriority = ["antigravity", "vscode"],
            Hotkeys =
            {
                ["ctrl+alt+win+o"] = "accept",
                ["ctrl+alt+win+p"] = "reject"
            },
            Settings = new AppSettingsConfig
            {
                AutoStartOnBoot = true,
                ShowTrayIcon = false
            }
        };
        original.Providers["antigravity"] = new ProviderConfig
        {
            Enabled = true,
            ProcessName = "antigravity",
            Actions =
            {
                ["accept"] = new ActionConfig { KeySequence = ["y", "Enter"] },
                ["reject"] = new ActionConfig { KeySequence = ["n"] }
            }
        };
        original.Providers["vscode"] = new ProviderConfig
        {
            Enabled = false,
            ProcessName = "Code",
            Actions =
            {
                ["accept"] = new ActionConfig { KeySequence = ["Tab"] }
            }
        };

        try
        {
            ConfigLoader.Save(original, tempFile);
            var loaded = ConfigLoader.Load(tempFile);

            Assert.Equal(original.Version, loaded.Version);
            Assert.Equal(original.ProviderPriority, loaded.ProviderPriority);
            Assert.Equal("accept", loaded.Hotkeys["ctrl+alt+win+o"]);
            Assert.Equal("reject", loaded.Hotkeys["ctrl+alt+win+p"]);
            Assert.True(loaded.Settings.AutoStartOnBoot);
            Assert.False(loaded.Settings.ShowTrayIcon);

            Assert.True(loaded.Providers["antigravity"].Enabled);
            Assert.Equal(["y", "Enter"], loaded.Providers["antigravity"].Actions["accept"].KeySequence);
            Assert.Equal(["n"], loaded.Providers["antigravity"].Actions["reject"].KeySequence);
            Assert.False(loaded.Providers["vscode"].Enabled);
            Assert.Equal("Code", loaded.Providers["vscode"].ProcessName);
            Assert.Equal(["Tab"], loaded.Providers["vscode"].Actions["accept"].KeySequence);
        }
        finally
        {
            if (File.Exists(tempFile))
            {
                File.Delete(tempFile);
            }
        }
    }

    [Fact]
    public void ConfigManager_Reload_PicksUpFileChanges()
    {
        var tempFile = Path.Combine(Path.GetTempPath(), $"ai-commander-test-{Guid.NewGuid():N}.yaml");
        var initial = new AICommanderConfig
        {
            Version = 1,
            ProviderPriority = ["antigravity"],
            Hotkeys = { ["ctrl+alt+win+o"] = "accept" }
        };
        initial.Providers["antigravity"] = new ProviderConfig
        {
            Enabled = true,
            ProcessName = "antigravity",
            Actions = { ["accept"] = new ActionConfig { KeySequence = ["y"] } }
        };

        try
        {
            ConfigLoader.Save(initial, tempFile);
            var manager = new ConfigManager(tempFile);
            Assert.Equal("accept", manager.CurrentConfig.Hotkeys["ctrl+alt+win+o"]);

            manager.CurrentConfig.Hotkeys["ctrl+alt+win+o"] = "reject";
            manager.CurrentConfig.Providers["antigravity"].Actions["accept"].KeySequence = ["n"];
            manager.Save();

            // Mutate in-memory state, then reload from disk
            manager.CurrentConfig.Hotkeys["ctrl+alt+win+o"] = "accept";
            manager.Reload();

            Assert.Equal("reject", manager.CurrentConfig.Hotkeys["ctrl+alt+win+o"]);
            Assert.Equal(["n"], manager.CurrentConfig.Providers["antigravity"].Actions["accept"].KeySequence);
        }
        finally
        {
            if (File.Exists(tempFile))
            {
                File.Delete(tempFile);
            }
        }
    }
}
