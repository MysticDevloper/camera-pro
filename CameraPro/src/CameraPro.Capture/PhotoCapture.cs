using System.IO;
using System.Windows.Media.Imaging;
using CameraPro.Core.Enums;
using CameraPro.Core.Interfaces;
using CameraPro.Core.Models;
using Serilog;

namespace CameraPro.Capture;

public enum ImageFormat
{
    Jpeg,
    Png,
    Bmp
}

public class PhotoCapture : IPhotoCapture
{
    private readonly ICameraManager _cameraManager;
    private string _baseSavePath;
    private int _photoCounter;
    private string _fileNamePrefix = "Photo";
    private ImageFormat _currentFormat = ImageFormat.Jpeg;
    private int _jpegQuality = 90;
    private Resolution _currentResolution;
    private int _burstIntervalMs = 100;
    private bool _useDateOrganization = true;

    public event EventHandler<string>? PhotoCaptured;
    public event EventHandler<int>? BurstProgress;
    public event EventHandler<int>? TimerCountdown;
    public event EventHandler<string>? CaptureError;

    public PhotoCapture(ICameraManager cameraManager)
    {
        _cameraManager = cameraManager;
        _baseSavePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.MyPictures),
            "CameraPro");
        Directory.CreateDirectory(_baseSavePath);
        _currentResolution = new Resolution { Width = 1920, Height = 1080 };
        LoadCounterFromExistingFiles();
    }

    public string CapturePhoto()
    {
        return CapturePhotoAsync().GetAwaiter().GetResult();
    }

    public async Task<string> CapturePhotoAsync(CancellationToken cancellationToken = default)
    {
        return await CapturePhotoWithSettingsAsync(_currentResolution, cancellationToken);
    }

    public string CapturePhotoWithSettings(Resolution resolution)
    {
        return CapturePhotoWithSettingsAsync(resolution).GetAwaiter().GetResult();
    }

    public async Task<string> CapturePhotoWithSettingsAsync(Resolution resolution, CancellationToken cancellationToken = default)
    {
        var frame = await Task.Run(() => _cameraManager.GetCurrentFrame(), cancellationToken);
        if (frame == null)
        {
            Log.Warning("No frame available for capture");
            CaptureError?.Invoke(this, "No frame available");
            return string.Empty;
        }

        var filePath = await GenerateFilePathAsync();
        return await SaveFrameAsync(frame, filePath, cancellationToken);
    }

    public List<string> CaptureBurst(int count)
    {
        return CaptureBurstAsync(count).GetAwaiter().GetResult();
    }

    public async Task<List<string>> CaptureBurstAsync(int count, CancellationToken cancellationToken = default)
    {
        var results = new List<string>();

        for (int i = 0; i < count; i++)
        {
            if (cancellationToken.IsCancellationRequested)
                break;

            var frame = await Task.Run(() => _cameraManager.GetCurrentFrame(), cancellationToken);
            if (frame == null)
            {
                Log.Warning("No frame available for burst capture at index {Index}", i);
                CaptureError?.Invoke(this, "No frame available");
                continue;
            }

            var filePath = await GenerateFilePathAsync();
            var saved = await SaveFrameAsync(frame, filePath, cancellationToken);
            if (!string.IsNullOrEmpty(saved))
            {
                results.Add(saved);
            }

            BurstProgress?.Invoke(this, i + 1);

            if (i < count - 1)
            {
                await Task.Delay(_burstIntervalMs, cancellationToken);
            }
        }

        Log.Information("Burst capture completed: {Count} photos", results.Count);
        return results;
    }

    public string CaptureWithDelay(int seconds)
    {
        return CaptureWithDelayAsync(seconds).GetAwaiter().GetResult();
    }

    public async Task<string> CaptureWithDelayAsync(int seconds, CancellationToken cancellationToken = default)
    {
        for (int i = seconds; i > 0; i--)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                Log.Information("Timer countdown cancelled");
                return string.Empty;
            }

            TimerCountdown?.Invoke(this, i);
            await Task.Delay(1000, cancellationToken);
        }

        return await CapturePhotoAsync(cancellationToken);
    }

    private void LoadCounterFromExistingFiles()
    {
        try
        {
            var savePath = GetSavePath();
            if (!Directory.Exists(savePath))
                return;

            var files = Directory.GetFiles(savePath, $"{_fileNamePrefix}_*.{GetFileExtension()}");
            int maxCounter = 0;

            foreach (var file in files)
            {
                var fileName = Path.GetFileNameWithoutExtension(file);
                var parts = fileName.Split('_');
                if (parts.Length > 0 && int.TryParse(parts[^1], out int counter))
                {
                    if (counter > maxCounter)
                        maxCounter = counter;
                }
            }

            _photoCounter = maxCounter + 1;
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Failed to load counter from existing files");
        }
    }

    private string GetSavePath()
    {
        if (!_useDateOrganization)
            return _baseSavePath;

        var now = DateTime.Now;
        return Path.Combine(_baseSavePath, now.Year.ToString(), $"{now.Month:D2}-{now:MMMM}");
    }

    private async Task<string> GenerateFilePathAsync()
    {
        var timestamp = DateTime.Now;
        var counter = _photoCounter++.ToString("D3");
        var extension = GetFileExtension();

        var fileName = _fileNamePrefix switch
        {
            "Photo" => $"Photo_{timestamp:yyyy-MM-dd}_{timestamp:HH-mm-ss}_{counter}.{extension}",
            _ => $"{_fileNamePrefix}_{timestamp:yyyy-MM-dd}_{timestamp:HH-mm-ss}_{counter}.{extension}"
        };

        var savePath = GetSavePath();
        Directory.CreateDirectory(savePath);

        return await Task.FromResult(Path.Combine(savePath, fileName));
    }

    private string GetFileExtension()
    {
        return _currentFormat switch
        {
            ImageFormat.Jpeg => "jpg",
            ImageFormat.Png => "png",
            ImageFormat.Bmp => "bmp",
            _ => "jpg"
        };
    }

    private async Task<string> SaveFrameAsync(BitmapSource frame, string filePath, CancellationToken cancellationToken = default)
    {
        try
        {
            BitmapEncoder encoder = _currentFormat switch
            {
                ImageFormat.Jpeg => new JpegBitmapEncoder { QualityLevel = _jpegQuality },
                ImageFormat.Png => new PngBitmapEncoder(),
                ImageFormat.Bmp => new BmpBitmapEncoder(),
                _ => new JpegBitmapEncoder { QualityLevel = _jpegQuality }
            };

            encoder.Frames.Add(BitmapFrame.Create(frame));

            await Task.Run(() =>
            {
                using var stream = new FileStream(filePath, FileMode.Create);
                encoder.Save(stream);
            }, cancellationToken);

            PhotoCaptured?.Invoke(this, filePath);
            Log.Information("Photo saved: {FilePath}", filePath);
            return filePath;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to save photo: {FilePath}", filePath);
            CaptureError?.Invoke(this, $"Failed to save photo: {ex.Message}");
            return string.Empty;
        }
    }

    public void SetFormat(ImageFormat format)
    {
        _currentFormat = format;
        Log.Information("Image format set to: {Format}", format);
    }

    public void SetJpegQuality(int quality)
    {
        _jpegQuality = Math.Clamp(quality, 1, 100);
        Log.Information("JPEG quality set to: {Quality}", _jpegQuality);
    }

    public void SetFileNamePrefix(string prefix)
    {
        _fileNamePrefix = string.IsNullOrWhiteSpace(prefix) ? "Photo" : prefix;
    }

    public void SetResolution(Resolution resolution)
    {
        _currentResolution = resolution;
    }

    public void SetBurstInterval(int milliseconds)
    {
        _burstIntervalMs = Math.Max(0, milliseconds);
    }

    public void SetSavePath(string path)
    {
        if (Directory.Exists(path))
        {
            _baseSavePath = path;
            Log.Information("Save path set to: {Path}", path);
        }
        else
        {
            try
            {
                Directory.CreateDirectory(path);
                _baseSavePath = path;
                Log.Information("Save path created and set to: {Path}", path);
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Failed to create save path: {Path}", path);
            }
        }
    }

    public void SetDateOrganization(bool enabled)
    {
        _useDateOrganization = enabled;
    }

    public ImageFormat GetCurrentFormat() => _currentFormat;
    public string GetSavePath() => _baseSavePath;
    public Resolution GetCurrentResolution() => _currentResolution;
    public int GetBurstInterval() => _burstIntervalMs;

    public BitmapSource? GetCurrentFrameForBurst()
    {
        return _cameraManager.GetCurrentFrame();
    }

    public string GenerateFilePathForBurst()
    {
        return GenerateFilePathAsync().GetAwaiter().GetResult();
    }

    public string SaveFrameForBurst(BitmapSource frame, string filePath)
    {
        return SaveFrameAsync(frame, filePath).GetAwaiter().GetResult();
    }

    public async Task<string> SaveFrameForBurstAsync(BitmapSource frame, string filePath, CancellationToken cancellationToken = default)
    {
        return await SaveFrameAsync(frame, filePath, cancellationToken);
    }
}
