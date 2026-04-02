using CameraPro.Core.Enums;
using CameraPro.Core.Interfaces;
using CameraPro.Core.Models;
using Serilog;

namespace CameraPro.Recording;

public class RecordingController : IDisposable
{
    private readonly IVideoRecorder _videoRecorder;
    private readonly ILogger _logger;
    private string _outputPath = string.Empty;
    private CaptureSettings _settings = new();
    private bool _disposed;

    private TimeSpan _currentDuration;
    private long _currentFileSize;
    private TimeSpan? _maxDuration = TimeSpan.FromMinutes(30);
    private long? _maxFileSize = 2L * 1024 * 1024 * 1024;
    private const long OneGB = 1_073_741_824;
    private const long WarningThreshold = 0.8;

    public event EventHandler<TimeSpan>? DurationUpdated;
    public event EventHandler<long>? FileSizeUpdated;
    public event EventHandler<RecordingSession>? RecordingComplete;
    public event EventHandler<string>? Warning;

    public bool IsRecording => _videoRecorder.IsRecording;
    public bool IsPaused => _videoRecorder.IsPaused;
    public RecordingStatus Status => _videoRecorder.Status;
    public TimeSpan CurrentDuration => _currentDuration;
    public long CurrentFileSize => _currentFileSize;

    public RecordingController(IVideoRecorder videoRecorder)
    {
        _videoRecorder = videoRecorder;
        _logger = Log.ForContext<RecordingController>();

        _videoRecorder.DurationUpdated += OnDurationUpdated;
        _videoRecorder.FileSizeUpdated += OnFileSizeUpdated;
        _videoRecorder.RecordingComplete += OnRecordingComplete;
    }

    private void OnDurationUpdated(object? sender, TimeSpan duration)
    {
        _currentDuration = duration;
        DurationUpdated?.Invoke(this, duration);

        CheckAutoStopLimits();
    }

    private void OnFileSizeUpdated(object? sender, long fileSize)
    {
        _currentFileSize = fileSize;
        FileSizeUpdated?.Invoke(this, fileSize);

        CheckStorageWarning();
    }

    private void OnRecordingComplete(object? sender, RecordingSession session)
    {
        _currentDuration = TimeSpan.Zero;
        _currentFileSize = 0;
    }

    public async Task<bool> StartRecordingAsync(string outputPath, CaptureSettings settings)
    {
        if (!CheckStorageSpace(outputPath))
        {
            _logger.Warning("Insufficient storage space");
            return false;
        }

        _outputPath = outputPath;
        _settings = settings;

        try
        {
            _videoRecorder.StartRecording(outputPath, settings);
            _logger.Information("Recording started: {Path}", outputPath);
            return true;
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to start recording");
            return false;
        }
    }

    public RecordingSession StopRecording()
    {
        var session = _videoRecorder.StopRecording();
        _logger.Information("Recording stopped: {Path}", session.FilePath);
        return session;
    }

    public void PauseRecording()
    {
        _videoRecorder.PauseRecording();
    }

    public void ResumeRecording()
    {
        _videoRecorder.ResumeRecording();
    }

    public (bool isRecording, TimeSpan duration, long fileSize) GetStatus()
    {
        return (IsRecording, _currentDuration, _currentFileSize);
    }

    private bool CheckStorageSpace(string path)
    {
        var drive = Path.GetPathRoot(path);
        if (string.IsNullOrEmpty(drive))
        {
            _logger.Warning("Could not determine drive for path: {Path}", path);
            return true;
        }

        try
        {
            var driveInfo = new DriveInfo(drive);
            var availableSpace = driveInfo.AvailableFreeSpace;

            if (availableSpace < OneGB)
            {
                Warning?.Invoke(this, "Warning: Less than 1GB available on disk");
                _logger.Warning("Insufficient disk space: {Available}MB available", availableSpace / (1024 * 1024));
                return false;
            }

            return true;
        }
        catch (Exception ex)
        {
            _logger.Warning(ex, "Could not check storage space");
            return true;
        }
    }

    private void CheckStorageWarning()
    {
        var drive = Path.GetPathRoot(_outputPath);
        if (string.IsNullOrEmpty(drive)) return;

        try
        {
            var driveInfo = new DriveInfo(drive);
            var usedPercentage = (double)(driveInfo.TotalSize - driveInfo.AvailableFreeSpace) / driveInfo.TotalSize;

            if (usedPercentage >= WarningThreshold)
            {
                Warning?.Invoke(this, $"Warning: Disk usage at {usedPercentage:P0}");
            }
        }
        catch { }
    }

    private void CheckAutoStopLimits()
    {
        if (_maxDuration.HasValue && _currentDuration >= _maxDuration.Value)
        {
            StopRecording();
            Warning?.Invoke(this, "Maximum recording duration reached");
            return;
        }
        
        if (_maxFileSize.HasValue && _currentFileSize >= _maxFileSize.Value)
        {
            StopRecording();
            Warning?.Invoke(this, "Maximum file size reached");
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        if (IsRecording)
        {
            StopRecording();
        }

        GC.SuppressFinalize(this);
    }
}