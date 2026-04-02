using CameraPro.Core.Models;
using CommunityToolkit.Mvvm.ComponentModel;

namespace CameraPro.Controls.Color;

public partial class ColorControls : ObservableObject
{
    [ObservableProperty]
    private double _brightness;

    [ObservableProperty]
    private double _contrast;

    [ObservableProperty]
    private double _saturation;

    [ObservableProperty]
    private double _hue;

    [ObservableProperty]
    private double _redGain = 1.0;

    [ObservableProperty]
    private double _greenGain = 1.0;

    [ObservableProperty]
    private double _blueGain = 1.0;

    public double MinValue => -100;
    public double MaxValue => 100;
    public double DefaultValue => 0;

    public double MinGain => 0.5;
    public double MaxGain => 2.0;
    public double DefaultGain => 1.0;

    public void Reset()
    {
        Brightness = DefaultValue;
        Contrast = DefaultValue;
        Saturation = DefaultValue;
        Hue = 0;
        RedGain = DefaultGain;
        GreenGain = DefaultGain;
        BlueGain = DefaultGain;
    }

    public void SetBrightness(double value)
    {
        Brightness = Math.Clamp(value, MinValue, MaxValue);
    }

    public void SetContrast(double value)
    {
        Contrast = Math.Clamp(value, MinValue, MaxValue);
    }

    public void SetSaturation(double value)
    {
        Saturation = Math.Clamp(value, MinValue, MaxValue);
    }

    public void SetHue(double value)
    {
        Hue = value % 360;
    }

    public void SetRedGain(double value)
    {
        RedGain = Math.Clamp(value, MinGain, MaxGain);
    }

    public void SetGreenGain(double value)
    {
        GreenGain = Math.Clamp(value, MinGain, MaxGain);
    }

    public void SetBlueGain(double value)
    {
        BlueGain = Math.Clamp(value, MinGain, MaxGain);
    }

    public void ResetRgbGains()
    {
        RedGain = DefaultGain;
        GreenGain = DefaultGain;
        BlueGain = DefaultGain;
    }

    public ControlCapability GetBrightnessCapability() => new()
    {
        ControlName = "Brightness",
        IsSupported = true,
        MinValue = MinValue,
        MaxValue = MaxValue,
        DefaultValue = DefaultValue,
        Step = 1
    };

    public ControlCapability GetContrastCapability() => new()
    {
        ControlName = "Contrast",
        IsSupported = true,
        MinValue = MinValue,
        MaxValue = MaxValue,
        DefaultValue = DefaultValue,
        Step = 1
    };

    public ControlCapability GetSaturationCapability() => new()
    {
        ControlName = "Saturation",
        IsSupported = true,
        MinValue = MinValue,
        MaxValue = MaxValue,
        DefaultValue = DefaultValue,
        Step = 1
    };

    public ControlCapability GetHueCapability() => new()
    {
        ControlName = "Hue",
        IsSupported = true,
        MinValue = 0,
        MaxValue = 360,
        DefaultValue = 0,
        Step = 1
    };

    public ControlCapability GetRedGainCapability() => new()
    {
        ControlName = "RedGain",
        IsSupported = true,
        MinValue = MinGain,
        MaxValue = MaxGain,
        DefaultValue = DefaultGain,
        Step = 0.1
    };

    public ControlCapability GetGreenGainCapability() => new()
    {
        ControlName = "GreenGain",
        IsSupported = true,
        MinValue = MinGain,
        MaxValue = MaxGain,
        DefaultValue = DefaultGain,
        Step = 0.1
    };

    public ControlCapability GetBlueGainCapability() => new()
    {
        ControlName = "BlueGain",
        IsSupported = true,
        MinValue = MinGain,
        MaxValue = MaxGain,
        DefaultValue = DefaultGain,
        Step = 0.1
    };
}