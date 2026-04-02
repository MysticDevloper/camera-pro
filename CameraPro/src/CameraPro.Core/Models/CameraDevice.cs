namespace CameraPro.Core.Models;

public class CameraDevice
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public bool IsDefault { get; set; }
    public List<Resolution> SupportedResolutions { get; set; } = new();
    public List<double> SupportedFrameRates { get; set; } = new();
}