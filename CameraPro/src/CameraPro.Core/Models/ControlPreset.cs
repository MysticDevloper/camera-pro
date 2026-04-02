namespace CameraPro.Core.Models;

public class ControlPreset
{
    public string Name { get; set; } = string.Empty;
    public double Exposure { get; set; }
    public double Focus { get; set; }
    public int WhiteBalance { get; set; }
    public double Brightness { get; set; }
    public double Contrast { get; set; }
    public double Saturation { get; set; }
    public double Zoom { get; set; }
    public bool IsAutoExposure { get; set; }
    public bool IsAutoFocus { get; set; }
    public bool IsAutoWhiteBalance { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public string? CameraId { get; set; }
}