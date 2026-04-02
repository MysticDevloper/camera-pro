using System.Text.Json;
using Serilog;

namespace CameraPro.Core.Infrastructure;

public class AppConfig
{
    private static readonly string ConfigPath = "config.json";
    public static AppConfig Current { get; private set; } = new();

    public string SavePath { get; set; } = "C:/Videos/CameraPro";
    public string PhotoPath { get; set; } = "C:/Pictures/CameraPro";
    public int DefaultFrameRate { get; set; } = 30;
    public string DefaultFormat { get; set; } = "MP4";
    public int VideoQuality { get; set; } = 8;

    public static void Load()
    {
        try
        {
            if (File.Exists(ConfigPath))
            {
                var json = File.ReadAllText(ConfigPath);
                Current = JsonSerializer.Deserialize<AppConfig>(json) ?? new AppConfig();
            }
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Failed to load config, using defaults");
        }
    }

    public static void Save()
    {
        try
        {
            var json = JsonSerializer.Serialize(Current, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(ConfigPath, json);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to save config");
        }
    }
}
