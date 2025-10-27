using System.IO;
using System.Text.Json;
using JISMemo.Models;

namespace JISMemo.Services;

public class SettingsService
{
    private const string SettingsFileName = "settings.json";
    
    private string GetSettingsFilePath()
    {
        var appDataPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "JISMemo");
        Directory.CreateDirectory(appDataPath);
        return Path.Combine(appDataPath, SettingsFileName);
    }

    public AppSettings LoadSettings()
    {
        try
        {
            var path = GetSettingsFilePath();
            if (!File.Exists(path))
            {
                var defaultSettings = new AppSettings();
                SaveSettings(defaultSettings);
                return defaultSettings;
            }
            
            var json = File.ReadAllText(path);
            return JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
        }
        catch
        {
            return new AppSettings();
        }
    }

    public void SaveSettings(AppSettings settings)
    {
        try
        {
            var path = GetSettingsFilePath();
            var json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(path, json);
        }
        catch { }
    }
}
