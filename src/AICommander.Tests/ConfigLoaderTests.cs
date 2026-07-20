using AICommander.Core.Config;

namespace AICommander.Tests;

public class ConfigLoaderTests
{
    [Fact]
    public void Load_ValidConfig_ReturnsDeserializedConfig()
    {
        // Arrange
        var tempFile = Path.GetTempFileName();
        var yaml = @"
version: 1
provider_priority:
  - antigravity
providers:
  antigravity:
    enabled: true
    process_name: antigravity
    actions:
      accept:
        key_sequence: [""y"", ""Enter""]
hotkeys:
  ""ctrl+alt+win+o"": ""accept""
";
        File.WriteAllText(tempFile, yaml);

        try
        {
            // Act
            var config = ConfigLoader.Load(tempFile);

            // Assert
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
        // Act & Assert
        Assert.Throws<FileNotFoundException>(() => ConfigLoader.Load("non_existent_file.yaml"));
    }
}
