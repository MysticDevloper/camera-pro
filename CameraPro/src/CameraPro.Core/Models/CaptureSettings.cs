using CameraPro.Core.Enums;

namespace CameraPro.Core.Models;

public class CaptureSettings
{
    public Resolution Resolution { get; set; } = new() { Width = 1920, Height = 1080 };
    public double FrameRate { get; set; } = 30.0;
    public VideoFormat Format { get; set; } = VideoFormat.MP4;
}