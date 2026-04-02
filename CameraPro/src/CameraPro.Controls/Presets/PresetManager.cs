using CameraPro.Core.Models;
using System.Text.Json;

namespace CameraPro.Controls.Presets;

public class PresetManager
{
    private readonly string _presetsFolder;
    private readonly Dictionary<string, ControlPreset> _presets = new();
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    public PresetManager()
    {
        _presetsFolder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "CameraPro",
            "Presets"
        );
        Directory.CreateDirectory(_presetsFolder);
        LoadFactoryPresets();
        LoadUserPresets();
    }

    private void LoadFactoryPresets()
    {
        _presets["Indoor"] = new ControlPreset
        {
            Name = "Indoor",
            WhiteBalance = 4000,
            Exposure = -10,
            Brightness = 10,
            Contrast = 10,
            Saturation = 10,
            IsAutoExposure = false,
            IsAutoWhiteBalance = false,
            IsAutoFocus = true
        };

        _presets["Outdoor"] = new ControlPreset
        {
            Name = "Outdoor",
            WhiteBalance = 5500,
            Exposure = 0,
            Brightness = 0,
            Contrast = 5,
            Saturation = 15,
            IsAutoExposure = true,
            IsAutoWhiteBalance = true,
            IsAutoFocus = true
        };

        _presets["LowLight"] = new ControlPreset
        {
            Name = "LowLight",
            WhiteBalance = 5500,
            Exposure = 30,
            Brightness = 30,
            Contrast = -10,
            Saturation = 5,
            IsAutoExposure = false,
            IsAutoWhiteBalance = false,
            IsAutoFocus = true
        };
    }

    private void LoadUserPresets()
    {
        if (!Directory.Exists(_presetsFolder)) return;

        foreach (var file in Directory.GetFiles(_presetsFolder, "*.json"))
        {
            try
            {
                var json = File.ReadAllText(file);
                var preset = JsonSerializer.Deserialize<ControlPreset>(json);
                if (preset != null)
                {
                    _presets[preset.Name] = preset;
                }
            }
            catch
            {
            }
        }
    }

    public IEnumerable<ControlPreset> GetAllPresets() => _presets.Values.ToList();

    public IEnumerable<ControlPreset> GetFactoryPresets() =>
        _presets.Values.Where(p => p.CameraId == null).ToList();

    public IEnumerable<ControlPreset> GetUserPresets() =>
        _presets.Values.Where(p => p.CameraId != null).ToList();

    public IEnumerable<ControlPreset> GetPresetsForCamera(string cameraId) =>
        _presets.Values.Where(p => p.CameraId == null || p.CameraId == cameraId).ToList();

    public ControlPreset? GetPreset(string name) =>
        _presets.TryGetValue(name, out var preset) ? preset : null;

    public void SavePreset(ControlPreset preset)
    {
        _presets[preset.Name] = preset;

        var filePath = Path.Combine(_presetsFolder, $"{preset.Name}.json");
        var json = JsonSerializer.Serialize(preset, JsonOptions);
        File.WriteAllText(filePath, json);
    }

    public void DeletePreset(string name)
    {
        if (_presets.Remove(name))
        {
            var filePath = Path.Combine(_presetsFolder, $"{name}.json");
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }
        }
    }

    public void SaveCurrentAsPreset(
        string name,
        string? cameraId,
        double exposure,
        double focus,
        int whiteBalance,
        double brightness,
        double contrast,
        double saturation,
        bool isAutoExposure,
        bool isAutoFocus,
        bool isAutoWhiteBalance)
    {
        var preset = new ControlPreset
        {
            Name = name,
            CameraId = cameraId,
            Exposure = exposure,
            Focus = focus,
            WhiteBalance = whiteBalance,
            Brightness = brightness,
            Contrast = contrast,
            Saturation = saturation,
            IsAutoExposure = isAutoExposure,
            IsAutoFocus = isAutoFocus,
            IsAutoWhiteBalance = isAutoWhiteBalance
        };

        SavePreset(preset);
    }
}