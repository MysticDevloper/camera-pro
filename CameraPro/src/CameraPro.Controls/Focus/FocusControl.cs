using CameraPro.Core.Enums;
using CameraPro.Core.Models;
using CommunityToolkit.Mvvm.ComponentModel;

namespace CameraPro.Controls.Focus;

public partial class FocusControl : ObservableObject
{
    [ObservableProperty]
    private double _focus = 0.5;

    [ObservableProperty]
    private FocusMode _mode = FocusMode.Auto;

    [ObservableProperty]
    private bool _isPeakingEnabled;

    [ObservableProperty]
    private double _nearFocus = 0;

    [ObservableProperty]
    private double _farFocus = 1;

    public double MinFocus => 0;
    public double MaxFocus => 1;
    public double DefaultFocus => 0.5;

    public bool SupportsPeaking => true;
    public bool SupportsContinuous => true;

    public void Reset()
    {
        Focus = DefaultFocus;
        Mode = FocusMode.Auto;
        IsPeakingEnabled = false;
    }

    public void SetMode(FocusMode mode)
    {
        Mode = mode;
    }

    public void SetModeAuto()
    {
        Mode = FocusMode.Auto;
    }

    public void SetModeManual()
    {
        Mode = FocusMode.Manual;
    }

    public void SetModeContinuous()
    {
        Mode = FocusMode.Continuous;
    }

    public void TogglePeaking()
    {
        IsPeakingEnabled = !IsPeakingEnabled;
    }

    public void SetFocusPoint(double normalizedPosition)
    {
        Focus = Math.Clamp(normalizedPosition, MinFocus, MaxFocus);
    }

    public ControlCapability GetCapability()
    {
        return new ControlCapability
        {
            ControlName = "Focus",
            IsSupported = true,
            MinValue = MinFocus,
            MaxValue = MaxFocus,
            DefaultValue = DefaultFocus,
            Step = 0.01,
            IsAutoSupported = true,
            IsManualSupported = true
        };
    }
}