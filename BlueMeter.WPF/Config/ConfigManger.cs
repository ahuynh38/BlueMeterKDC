using System.IO;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace BlueMeter.WPF.Config;

public class ConfigManger : IConfigManager
{
    private readonly string _configFilePath;
    private readonly JsonSerializerOptions _jsonOptions;
    private readonly IOptionsMonitor<AppConfig> _optionsMonitor;

    public ConfigManger(IOptionsMonitor<AppConfig> optionsMonitor,
        IOptions<JsonSerializerOptions> jsonOptions)
    {
        _optionsMonitor = optionsMonitor;
        _jsonOptions = jsonOptions.Value;

        // Save user settings to AppData to persist across updates
        var appDataPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "BlueMeter");
        Directory.CreateDirectory(appDataPath);
        _configFilePath = Path.Combine(appDataPath, "config.json");

        // Subscribe to configuration changes
        _optionsMonitor.OnChange(OnConfigurationChanged);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="newConfig"></param>
    /// <returns></returns>
    /// <exception cref="InvalidOperationException"></exception>
    public async Task SaveAsync(AppConfig? newConfig = null)
    {
        try
        {
            newConfig ??= CurrentConfig;

            // Save only the Config section to AppData
            // This file will overlay the default appsettings.json
            var rootDict = new Dictionary<string, object>
            {
                ["Config"] = newConfig
            };

            // Write to AppData config file
            var updatedJson = JsonSerializer.Serialize(rootDict, _jsonOptions);
            await File.WriteAllTextAsync(_configFilePath, updatedJson);

            // Force configuration reload (the file watcher should pick this up automatically)
            // But we can also manually notify if needed
            OnConfigurationChanged(newConfig);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to update configuration: {ex.Message}", ex);
        }
    }

    public event EventHandler<AppConfig>? ConfigurationUpdated;

    public AppConfig CurrentConfig => _optionsMonitor.CurrentValue;

    private void OnConfigurationChanged(AppConfig newConfig)
    {
        ConfigurationUpdated?.Invoke(this, newConfig);
    }
}