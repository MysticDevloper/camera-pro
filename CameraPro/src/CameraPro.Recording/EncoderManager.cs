using CameraPro.Core.Enums;

namespace CameraPro.Recording;

public enum QualityPreset
{
    Low,
    Medium,
    High,
    Ultra
}

public enum BitrateMode
{
    CBR,
    VBR
}

public class EncoderSettings
{
    public QualityPreset Quality { get; set; } = QualityPreset.Medium;
    public BitrateMode BitrateMode { get; set; } = BitrateMode.VBR;
    public string VideoCodec { get; set; } = "libx264";
    public string AudioCodec { get; set; } = "aac";
    public int VideoBitrate { get; set; } = 8_000_000;
    public int AudioBitrate { get; set; } = 128_000;
    public int AudioSampleRate { get; set; } = 44100;
}

public class EncoderManager
{
    private static readonly Dictionary<QualityPreset, int> QualityBitrates = new()
    {
        { QualityPreset.Low, 2_000_000 },
        { QualityPreset.Medium, 8_000_000 },
        { QualityPreset.High, 20_000_000 },
        { QualityPreset.Ultra, 50_000_000 }
    };

    public EncoderSettings GetSettings(QualityPreset preset, BitrateMode mode = BitrateMode.VBR)
    {
        var settings = new EncoderSettings
        {
            Quality = preset,
            BitrateMode = mode,
            VideoBitrate = QualityBitrates[preset],
            AudioBitrate = preset switch
            {
                QualityPreset.Low => 64_000,
                QualityPreset.Medium => 128_000,
                QualityPreset.High => 192_000,
                QualityPreset.Ultra => 256_000,
                _ => 128_000
            }
        };
        return settings;
    }

    public string GetCodecExtension(VideoFormat format)
    {
        return format switch
        {
            VideoFormat.MP4 => ".mp4",
            VideoFormat.MKV => ".mkv",
            VideoFormat.AVI => ".avi",
            VideoFormat.WEBM => ".webm",
            _ => ".mp4"
        };
    }

    public string BuildFFmpegArgs(EncoderSettings settings, int width, int height, double frameRate, string outputPath)
    {
        var videoBitrate = settings.VideoBitrate / 1000;
        var audioBitrate = settings.AudioBitrate / 1000;

        var rateControl = settings.BitrateMode == BitrateMode.CBR
            ? $"-b:v {videoBitrate}k -minrate {videoBitrate}k -maxrate {videoBitrate}k"
            : $"-b:v {videoBitrate}k";

        // CRITICAL: -f rawvideo tells FFmpeg to expect raw bytes
        return $"-f rawvideo -vcodec rawvideo -s {width}x{height} -r {frameRate} -pix_fmt yuv420p -i - " +
               $"-c:v {settings.VideoCodec} {rateControl} " +
               $"-pix_fmt yuv420p -y \"{outputPath}\"";
    }
}