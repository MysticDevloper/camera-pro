using CameraPro.Core.Models;
using CommunityToolkit.Mvvm.ComponentModel;

namespace CameraPro.Controls.Zoom;

public partial class ZoomControl : ObservableObject
{
    [ObservableProperty]
    private double _zoom = 1.0;

    [ObservableProperty]
    private double _minZoom = 1.0;

    [ObservableProperty]
    private double _maxZoom = 10.0;

    public double DefaultZoom => 1.0;
    public double Step => 0.1;

    public void Reset()
    {
        Zoom = DefaultZoom;
    }

    public void SetZoom(double value)
    {
        Zoom = Math.Clamp(value, MinZoom, MaxZoom);
    }

    public void ZoomIn()
    {
        Zoom = Math.Min(Zoom + Step, MaxZoom);
    }

    public void ZoomOut()
    {
        Zoom = Math.Max(Zoom - Step, MinZoom);
    }

    public void SetMaxZoom(double max)
    {
        MaxZoom = Math.Max(1.0, max);
    }

    public ControlCapability GetCapability()
    {
        return new ControlCapability
        {
            ControlName = "Zoom",
            IsSupported = true,
            MinValue = MinZoom,
            MaxValue = MaxZoom,
            DefaultValue = DefaultZoom,
            Step = Step
        };
    }
}