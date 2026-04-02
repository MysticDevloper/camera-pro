using CameraPro.Core.Enums;
using CameraPro.Core.Models;
using CommunityToolkit.Mvvm.ComponentModel;

namespace CameraPro.Controls.WhiteBalance;

public partial class WhiteBalanceControl : ObservableObject
{
    [ObservableProperty]
    private int _whiteBalance = 5500;

    [ObservableProperty]
    private WhiteBalancePreset _preset = WhiteBalancePreset.Auto;

    [ObservableProperty]
    private bool _isLocked;

    [ObservableProperty]
    private bool _isAuto = true;

    public int MinWhiteBalance => 2000;
    public int MaxWhiteBalance => 10000;
    public int DefaultWhiteBalance => 5500;
    public int Step => 100;

    public static IReadOnlyList<WhiteBalancePreset> AvailablePresets =>
    [
        WhiteBalancePreset.Auto,
        WhiteBalancePreset.Daylight,
        WhiteBalancePreset.Cloudy,
        WhiteBalancePreset.Tungsten,
        WhiteBalancePreset.Fluorescent,
        WhiteBalancePreset.Manual
    ];

    public static IReadOnlyDictionary<WhiteBalancePreset, int> PresetToKelvin => new Dictionary<WhiteBalancePreset, int>
    {
        { WhiteBalancePreset.Daylight, 5500 },
        { WhiteBalancePreset.Cloudy, 6500 },
        { WhiteBalancePreset.Tungsten, 2800 },
        { WhiteBalancePreset.Fluorescent, 4000 },
        { WhiteBalancePreset.Manual, 5500 }
    };

    public void Reset()
    {
        WhiteBalance = DefaultWhiteBalance;
        Preset = WhiteBalancePreset.Auto;
        IsLocked = false;
        IsAuto = true;
    }

    public void SetPreset(WhiteBalancePreset preset)
    {
        Preset = preset;
        IsAuto = preset == WhiteBalancePreset.Auto;

        if (preset != WhiteBalancePreset.Auto && preset != WhiteBalancePreset.Manual)
        {
            WhiteBalance = PresetToKelvin[preset];
        }
    }

    public void SetAuto(bool auto)
    {
        IsAuto = auto;
        if (auto)
        {
            Preset = WhiteBalancePreset.Auto;
        }
    }

    public void SetLocked(bool locked)
    {
        IsLocked = locked;
    }

    public void SetManualKelvin(int kelvin)
    {
        WhiteBalance = Math.Clamp(kelvin, MinWhiteBalance, MaxWhiteBalance);
        Preset = WhiteBalancePreset.Manual;
        IsAuto = false;
    }

    public ControlCapability GetCapability()
    {
        return new ControlCapability
        {
            ControlName = "WhiteBalance",
            IsSupported = true,
            MinValue = MinWhiteBalance,
            MaxValue = MaxWhiteBalance,
            DefaultValue = DefaultWhiteBalance,
            Step = Step,
            IsAutoSupported = true,
            IsManualSupported = true
        };
    }
}