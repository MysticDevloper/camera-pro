using CameraPro.Core.Interfaces;
using Serilog;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;

namespace CameraPro.Storage;

public class ThumbnailGenerator
{
    private readonly IFileManager _fileManager;
    private readonly string _cachePath;
    private readonly ILogger _logger;
    private readonly string _ffmpegPath;

    public ThumbnailGenerator(IFileManager fileManager)
    {
        _fileManager = fileManager;
        _cachePath = Path.Combine(Path.GetTempPath(), "CameraPro", "Thumbnails");
        _logger = Log.ForContext<ThumbnailGenerator>();
        _ffmpegPath = FindFFmpeg();

        EnsureDirectoryExists(_cachePath);
    }

    public async Task<byte[]?> GenerateImageThumbnailAsync(string imagePath, int maxWidth = 160)
    {
        if (!File.Exists(imagePath))
        {
            return null;
        }

        return await Task.Run(() =>
        {
            try
            {
                using var image = Image.FromFile(imagePath);
                var ratio = maxWidth / (double)image.Width;
                var newHeight = (int)(image.Height * ratio);

                using var thumbnail = image.GetThumbnailImage(maxWidth, newHeight, () => false, IntPtr.Zero);
                using var ms = new MemoryStream();
                thumbnail.Save(ms, ImageFormat.Jpeg);
                return ms.ToArray();
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Error generating image thumbnail: {Path}", imagePath);
                return null;
            }
        });
    }

    public async Task<string> GenerateVideoThumbnailAsync(string videoPath, TimeSpan? position = null)
    {
        var thumbnailFileName = $"{Path.GetFileNameWithoutExtension(videoPath)}.jpg";
        var thumbnailPath = Path.Combine(_cachePath, thumbnailFileName);

        if (File.Exists(thumbnailPath))
        {
            return thumbnailPath;
        }

        if (!string.IsNullOrEmpty(_ffmpegPath))
        {
            var timestamp = position ?? TimeSpan.Zero;
            var args = $"-i \"{videoPath}\" -ss {timestamp:hh\\:mm\\:ss\\.} -vframes 1 -q:v 2 -y \"{thumbnailPath}\"";

            try
            {
                var startInfo = new ProcessStartInfo
                {
                    FileName = _ffmpegPath,
                    Arguments = args,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };

                using var process = Process.Start(startInfo);
                if (process != null)
                {
                    await process.WaitForExitAsync();
                    if (process.ExitCode == 0 && File.Exists(thumbnailPath))
                    {
                        _logger.Information("Generated video thumbnail: {Path}", thumbnailPath);
                        return thumbnailPath;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.Warning(ex, "FFmpeg thumbnail generation failed for: {Path}", videoPath);
            }
        }

        _logger.Information("Using placeholder video thumbnail: {Path}", videoPath);
        return thumbnailPath;
    }

    public async Task<byte[]?> GenerateVideoThumbnailAsBytesAsync(string videoPath, TimeSpan? position = null)
    {
        var thumbnailPath = await GenerateVideoThumbnailAsync(videoPath, position);
        if (File.Exists(thumbnailPath))
        {
            return await File.ReadAllBytesAsync(thumbnailPath);
        }
        return null;
    }

    public async Task SaveThumbnailAsync(string mediaFileId, byte[] thumbnailData)
    {
        var thumbnailFileName = $"{mediaFileId}.jpg";
        var thumbnailPath = Path.Combine(_cachePath, thumbnailFileName);

        await File.WriteAllBytesAsync(thumbnailPath, thumbnailData);
        _logger.Information("Saved thumbnail: {Path}", thumbnailPath);
    }

    public async Task<byte[]?> LoadThumbnailAsync(string mediaFileId)
    {
        var thumbnailFileName = $"{mediaFileId}.jpg";
        var thumbnailPath = Path.Combine(_cachePath, thumbnailFileName);

        if (File.Exists(thumbnailPath))
        {
            return await File.ReadAllBytesAsync(thumbnailPath);
        }

        return null;
    }

    public async Task ClearCacheAsync()
    {
        await Task.Run(() =>
        {
            if (Directory.Exists(_cachePath))
            {
                foreach (var file in Directory.GetFiles(_cachePath, "*.jpg"))
                {
                    try
                    {
                        File.Delete(file);
                    }
                    catch (Exception ex)
                    {
                        _logger.Warning(ex, "Error deleting thumbnail: {Path}", file);
                    }
                }
            }
        });

        _logger.Information("Cleared thumbnail cache");
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

    private void EnsureDirectoryExists(string path)
    {
        if (!Directory.Exists(path))
        {
            Directory.CreateDirectory(path);
        }
    }
}
