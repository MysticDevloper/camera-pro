using CameraPro.Capture;

namespace CameraPro.Core.Models;

public class PhotoCaptureSettings
{
    public Resolution Resolution { get; set; } = new() { Width = 1920, Height = 1080 };
    public ImageFormat Format { get; set; } = ImageFormat.Jpeg;
    public int JpegQuality { get; set; } = 90;
    public string SavePath { get; set; } = string.Empty;
    public string FileNamePrefix { get; set; } = "Photo";
}

public class ResolutionPreset
{
    public static Resolution FourK => new() { Width = 3840, Height = 2160 };
    public static Resolution FullHD => new() { Width = 1920, Height = 1080 };
    public static Resolution HD => new() { Width = 1280, Height = 720 };
    public static Resolution SD => new() { Width = 640, Height = 480 };

    public static List<Resolution> GetPresets() => new() { FourK, FullHD, HD, SD };
}

public class AspectRatio
{
    public static readonly (int Width, int Height)[] Ratios = 
    {
        (16, 9),
        (4, 3),
        (1, 1)
    };
}
