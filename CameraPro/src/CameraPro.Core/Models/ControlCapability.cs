namespace CameraPro.Core.Models;

public class ControlCapability
{
    public string ControlName { get; set; } = string.Empty;
    public bool IsSupported { get; set; }
    public double MinValue { get; set; }
    public double MaxValue { get; set; }
    public double DefaultValue { get; set; }
    public double Step { get; set; }
    public bool IsAutoSupported { get; set; }
    public bool IsManualSupported { get; set; }
}