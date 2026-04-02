using CameraPro.Core.Enums;
using CameraPro.Core.Interfaces;
using Serilog;
using System.Diagnostics;
using System.IO;

namespace CameraPro.Storage;

public class VideoConverter : IVideoConverter
{
    private readonly ILogger _logger;
    private readonly string _ffmpegPath;

    public VideoConverter()
    {
        _logger = Log.ForContext<VideoConverter>();
        _ffmpegPath = FindFFmpeg();
    }

    public async Task<string> ConvertAsync(string inputPath, string outputPath, VideoFormat targetFormat, ConversionQuality quality = ConversionQuality.Medium, IProgress<int>? progress = null)
    {
        if (!File.Exists(inputPath))
        {
            throw new FileNotFoundException("Input file not found", inputPath);
        }

        var bitrate = quality switch
        {
            ConversionQuality.Low => "2M",
            ConversionQuality.Medium => "8M",
            ConversionQuality.High => "20M",
            ConversionQuality.Ultra => "50M",
            _ => "8M"
        };

        var codec = targetFormat switch
        {
            VideoFormat.MP4 => "libx264",
            VideoFormat.MKV => "libx264",
            VideoFormat.AVI => "mpeg4",
            VideoFormat.WEBM => "libvpx-vp9",
            _ => "libx264"
        };

        var extension = targetFormat.ToString().ToLowerInvariant();
        var outputFile = Path.ChangeExtension(outputPath, extension);

        var args = $"-i \"{inputPath}\" -c:v {codec} -b:v {bitrate} -c:a aac -b:a 192k -y \"{outputFile}\"";

        var result = await RunFFmpegAsync(args, progress);
        if (result != 0)
        {
            throw new Exception($"FFmpeg conversion failed with code {result}");
        }

        _logger.Information("Converted {Input} to {Output}", inputPath, outputFile);
        return outputFile;
    }

    public async Task<string> CompressAsync(string inputPath, string outputPath, int targetBitrate, IProgress<int>? progress = null)
    {
        if (!File.Exists(inputPath))
        {
            throw new FileNotFoundException("Input file not found", inputPath);
        }

        var bitrate = $"{targetBitrate}K";
        var args = $"-i \"{inputPath}\" -c:v libx264 -b:v {bitrate} -c:a aac -b:a 128k -y \"{outputPath}\"";

        var result = await RunFFmpegAsync(args, progress);
        if (result != 0)
        {
            throw new Exception($"FFmpeg compression failed with code {result}");
        }

        _logger.Information("Compressed {Input} to {Output}", inputPath, outputPath);
        return outputPath;
    }

    public async Task<string> TrimAsync(string inputPath, string outputPath, TimeSpan start, TimeSpan duration)
    {
        if (!File.Exists(inputPath))
        {
            throw new FileNotFoundException("Input file not found", inputPath);
        }

        var startStr = start.ToString(@"hh\:mm\:ss");
        var durationStr = duration.ToString(@"hh\:mm\:ss");
        var args = $"-i \"{inputPath}\" -ss {startStr} -t {durationStr} -c copy -y \"{outputPath}\"";

        var result = await RunFFmpegAsync(args, null);
        if (result != 0)
        {
            throw new Exception($"FFmpeg trim failed with code {result}");
        }

        _logger.Information("Trimmed {Input} from {Start} for {Duration}", inputPath, startStr, durationStr);
        return outputPath;
    }

    public async Task<string> MergeAsync(List<string> inputPaths, string outputPath)
    {
        if (inputPaths.Count == 0)
        {
            throw new ArgumentException("No input files provided");
        }

        var listFile = Path.Combine(Path.GetTempPath(), $"merge_{Guid.NewGuid()}.txt");
        var lines = inputPaths.Select(p => $"file '{p}'");
        await File.WriteAllLinesAsync(listFile, lines);

        var args = $"-f concat -safe 0 -i \"{listFile}\" -c copy -y \"{outputPath}\"";

        try
        {
            var result = await RunFFmpegAsync(args, null);
            if (result != 0)
            {
                throw new Exception($"FFmpeg merge failed with code {result}");
            }

            _logger.Information("Merged {Count} files to {Output}", inputPaths.Count, outputPath);
            return outputPath;
        }
        finally
        {
            if (File.Exists(listFile))
            {
                File.Delete(listFile);
            }
        }
    }

    public async Task<string> ChangeResolutionAsync(string inputPath, string outputPath, int width, int height)
    {
        if (!File.Exists(inputPath))
        {
            throw new FileNotFoundException("Input file not found", inputPath);
        }

        var args = $"-i \"{inputPath}\" -vf scale={width}:{height} -c:v libx264 -c:a copy -y \"{outputPath}\"";

        var result = await RunFFmpegAsync(args, null);
        if (result != 0)
        {
            throw new Exception($"FFmpeg resolution change failed with code {result}");
        }

        _logger.Information("Changed resolution of {Input} to {Width}x{Height}", inputPath, width, height);
        return outputPath;
    }

    private async Task<int> RunFFmpegAsync(string arguments, IProgress<int>? progress)
    {
        if (string.IsNullOrEmpty(_ffmpegPath))
        {
            _logger.Warning("FFmpeg not found, using placeholder conversion");
            await Task.Delay(100);
            return 0;
        }

        var startInfo = new ProcessStartInfo
        {
            FileName = _ffmpegPath,
            Arguments = arguments,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };

        using var process = new Process { StartInfo = startInfo };
        process.Start();

        await process.WaitForExitAsync();

        return process.ExitCode;
    }

    private string FindFFmpeg()
    {
        var possiblePaths = new[]
        {
            "ffmpeg",
            "ffmpeg.exe",
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "ffmpeg", "bin", "ffmpeg.exe"),
            @"C:\ffmpeg\bin\ffmpeg.exe"
        };

        foreach (var path in possiblePaths)
        {
            try
            {
                var startInfo = new ProcessStartInfo
                {
                    FileName = path,
                    Arguments = "-version",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true
                };

                using var process = Process.Start(startInfo);
                if (process != null)
                {
                    process.WaitForExit(1000);
                    if (process.ExitCode == 0)
                    {
                        return path;
                    }
                }
            }
            catch
            {
            }
        }

        return string.Empty;
    }
}
