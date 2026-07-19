using System;
using System.IO;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace AICommander.Core.Config;

public static class ConfigLoader
{
    public static AICommanderConfig Load(string filePath)
    {
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException($"Configuration file not found: {filePath}");
        }

        var yaml = File.ReadAllText(filePath);
        var deserializer = new DeserializerBuilder()
            .WithNamingConvention(UnderscoredNamingConvention.Instance)
            .Build();

        var config = deserializer.Deserialize<AICommanderConfig>(yaml);

        if (config == null)
        {
            throw new InvalidDataException("Failed to deserialize configuration file.");
        }

        return config;
    }
}
