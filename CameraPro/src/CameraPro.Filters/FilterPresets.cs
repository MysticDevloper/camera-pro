using System.Text.Json;
using System.IO;
using CameraPro.Core.Models;

namespace CameraPro.Filters;

public class FilterPresets
{
    private readonly string _presetsDirectory;
    private Dictionary<string, PresetData> _builtInPresets;
    private Dictionary<string, PresetData> _customPresets;

    public FilterPresets()
    {
        _presetsDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "CameraPro", "presets");
        Directory.CreateDirectory(_presetsDirectory);
        
        _builtInPresets = InitializeBuiltInPresets();
        _customPresets = new Dictionary<string, PresetData>();
        LoadCustomPresets();
    }

    private Dictionary<string, PresetData> InitializeBuiltInPresets()
    {
        return new Dictionary<string, PresetData>
        {
            ["Portrait"] = new PresetData
            {
                Name = "Portrait",
                Description = "Soft contrast, warm tones for portraits",
                Filters = new List<FilterConfig>
                {
                    new() { Type = "Adjustments", Parameters = new Dictionary<string, double> { ["Brightness"] = 5, ["Contrast"] = -10, ["Saturation"] = 10 } },
                    new() { Type = "ColorFilters", Parameters = new Dictionary<string, double> { ["Type"] = 4, ["Intensity"] = 0.3 } },
                    new() { Type = "EffectsProcessor", Parameters = new Dictionary<string, double> { ["EffectType"] = 12, ["Intensity"] = 0.3 } }
                }
            },
            ["Landscape"] = new PresetData
            {
                Name = "Landscape",
                Description = "High saturation, vivid colors for landscapes",
                Filters = new List<FilterConfig>
                {
                    new() { Type = "Adjustments", Parameters = new Dictionary<string, double> { ["Brightness"] = 10, ["Contrast"] = 15, ["Saturation"] = 40 } },
                    new() { Type = "ColorFilters", Parameters = new Dictionary<string, double> { ["Type"] = 10, ["Intensity"] = 0.2 } }
                }
            },
            ["Night"] = new PresetData
            {
                Name = "Night",
                Description = "Brightness boost with noise reduction",
                Filters = new List<FilterConfig>
                {
                    new() { Type = "Adjustments", Parameters = new Dictionary<string, double> { ["Brightness"] = 30, ["Contrast"] = 20, ["Gamma"] = 1.2 } },
                    new() { Type = "EffectsProcessor", Parameters = new Dictionary<string, double> { ["EffectType"] = 2, ["Intensity"] = 0.2 } }
                }
            },
            ["Vintage"] = new PresetData
            {
                Name = "Vintage",
                Description = "Sepia tones with vignette for classic look",
                Filters = new List<FilterConfig>
                {
                    new() { Type = "ColorFilters", Parameters = new Dictionary<string, double> { ["Type"] = 1, ["Intensity"] = 0.8 } },
                    new() { Type = "Adjustments", Parameters = new Dictionary<string, double> { ["Contrast"] = 10, ["Saturation"] = -20 } },
                    new() { Type = "EffectsProcessor", Parameters = new Dictionary<string, double> { ["EffectType"] = 12, ["Intensity"] = 0.6 } }
                }
            },
            ["BlackWhite"] = new PresetData
            {
                Name = "BlackWhite",
                Description = "Classic black and white",
                Filters = new List<FilterConfig>
                {
                    new() { Type = "ColorFilters", Parameters = new Dictionary<string, double> { ["Type"] = 0, ["Method"] = 1, ["Intensity"] = 1.0 } },
                    new() { Type = "Adjustments", Parameters = new Dictionary<string, double> { ["Contrast"] = 20 } }
                }
            },
            ["HighContrast"] = new PresetData
            {
                Name = "HighContrast",
                Description = "Bold, dramatic look",
                Filters = new List<FilterConfig>
                {
                    new() { Type = "ColorFilters", Parameters = new Dictionary<string, double> { ["Type"] = 11, ["Intensity"] = 1.0 } },
                    new() { Type = "Adjustments", Parameters = new Dictionary<string, double> { ["Contrast"] = 50, ["Brightness"] = -5 } }
                }
            },
            ["CoolBlue"] = new PresetData
            {
                Name = "CoolBlue",
                Description = "Cool blue tones for a calm effect",
                Filters = new List<FilterConfig>
                {
                    new() { Type = "ColorFilters", Parameters = new Dictionary<string, double> { ["Type"] = 10, ["Intensity"] = 0.7 } },
                    new() { Type = "Adjustments", Parameters = new Dictionary<string, double> { ["Saturation"] = 10, ["Contrast"] = 5 } }
                }
            },
            ["WarmSunset"] = new PresetData
            {
                Name = "WarmSunset",
                Description = "Warm orange tones for sunset",
                Filters = new List<FilterConfig>
                {
                    new() { Type = "ColorFilters", Parameters = new Dictionary<string, double> { ["Type"] = 9, ["Intensity"] = 0.8 } },
                    new() { Type = "Adjustments", Parameters = new Dictionary<string, double> { ["Saturation"] = 20, ["Hue"] = 20 } }
                }
            }
        };
    }

    private void LoadCustomPresets()
    {
        var filePath = Path.Combine(_presetsDirectory, "custom_presets.json");
        if (File.Exists(filePath))
        {
            try
            {
                var json = File.ReadAllText(filePath);
                _customPresets = JsonSerializer.Deserialize<Dictionary<string, PresetData>>(json) ?? new Dictionary<string, PresetData>();
            }
            catch
            {
                _customPresets = new Dictionary<string, PresetData>();
            }
        }
    }

    private void SaveCustomPresets()
    {
        var filePath = Path.Combine(_presetsDirectory, "custom_presets.json");
        var json = JsonSerializer.Serialize(_customPresets, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(filePath, json);
    }

    public IEnumerable<string> GetBuiltInPresetNames() => _builtInPresets.Keys;

    public IEnumerable<string> GetCustomPresetNames() => _customPresets.Keys;

    public PresetData? GetPreset(string name)
    {
        if (_builtInPresets.TryGetValue(name, out var builtIn))
            return builtIn;
        
        if (_customPresets.TryGetValue(name, out var custom))
            return custom;
        
        return null;
    }

    public IEnumerable<PresetData> GetAllPresets()
    {
        var all = new List<PresetData>(_builtInPresets.Values);
        all.AddRange(_customPresets.Values);
        return all;
    }

    public void SaveCustomPreset(string name, PresetData preset)
    {
        preset.Name = name;
        _customPresets[name] = preset;
        SaveCustomPresets();
    }

    public bool DeleteCustomPreset(string name)
    {
        if (_customPresets.Remove(name))
        {
            SaveCustomPresets();
            return true;
        }
        return false;
    }

    public string ExportPreset(string name, string filePath)
    {
        var preset = GetPreset(name);
        if (preset == null)
            throw new ArgumentException($"Preset '{name}' not found");
        
        var json = JsonSerializer.Serialize(preset, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(filePath, json);
        
        return filePath;
    }

    public PresetData? ImportPreset(string filePath)
    {
        var json = File.ReadAllText(filePath);
        var preset = JsonSerializer.Deserialize<PresetData>(json);
        
        if (preset != null && !string.IsNullOrEmpty(preset.Name))
        {
            preset.Name = Path.GetFileNameWithoutExtension(filePath);
            SaveCustomPreset(preset.Name, preset);
        }
        
        return preset;
    }
}

public class PresetData
{
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public List<FilterConfig> Filters { get; set; } = new();
}

public class FilterConfig
{
    public string Type { get; set; } = "";
    public Dictionary<string, double> Parameters { get; set; } = new();
}