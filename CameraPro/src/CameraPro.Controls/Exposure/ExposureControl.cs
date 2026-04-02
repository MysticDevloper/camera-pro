using CameraPro.Core.Enums;
using CameraPro.Core.Interfaces;
using CommunityToolkit.Mvvm.ComponentModel;

namespace CameraPro.Controls.Exposure;

public partial class ExposureControl : ObservableObject
{
    [ObservableProperty]
    private double _exposure = 0;

    [ObservableProperty]
    private bool _isAuto = true;

    [ObservableProperty]
    private bool _isLocked;

    [ObservableProperty]
    private int _iso = 100;

    [ObservableProperty]
    private double _exposureCompensation = 0;

    [ObservableProperty]
    private ExposureMode _mode = ExposureMode.Auto;

    public double MinExposure => -100;
    public double MaxExposure => 100;
    public double DefaultExposure => 0;

    public double MinExposureCompensation => -2;
    public double MaxExposureCompensation => 2;

    public int[] AvailableIsoValues => [100, 200, 400, 800, 1600, 3200, 6400];

    public bool SupportsIso => true;
    public bool SupportsCompensation => true;

    public void Reset()
    {
        Exposure = DefaultExposure;
        IsAuto = true;
        IsLocked = false;
        Iso = 100;
        ExposureCompensation = 0;
        Mode = ExposureMode.Auto;
    }

    public void SetAuto(bool auto)
    {
        IsAuto = auto;
        Mode = auto ? ExposureMode.Auto : ExposureMode.Manual;
    }

    public void SetLocked(bool locked)
    {
        IsLocked = locked;
        if (locked)
        {
            Mode = ExposureMode.Locked;
        }
    }

    public ControlCapability GetCapability()
    {
        return new Core.Models.ControlCapability
        {
            ControlName = "Exposure",
            IsSupported = true,
            MinValue = MinExposure,
            MaxValue = MaxExposure,
            DefaultValue = DefaultExposure,
            Step = 1,
            IsAutoSupported = true,
            IsManualSupported = true
        };
    }
}