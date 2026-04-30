using System.Text.Json;
using DumpUtility.Models;

namespace DumpUtility.Services;

public class ConfigService
{
    public AppConfig Load(string configPath)
    {
        if (!File.Exists(configPath))
            throw new FileNotFoundException($"config.json not found at: {configPath}");

        string json = File.ReadAllText(configPath);
        var config = JsonSerializer.Deserialize<AppConfig>(json,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        if (config is null)
            throw new InvalidOperationException("Failed to deserialize config.json.");

        if (string.IsNullOrWhiteSpace(config.BaseFolder))
            throw new InvalidOperationException("BaseFolder is required in config.json.");

        return config;
    }
}
