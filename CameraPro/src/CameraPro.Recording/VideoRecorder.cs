using CameraPro.Core.Enums;
using CameraPro.Core.Interfaces;
using CameraPro.Core.Models;
using CameraPro.Recording.StateMachine;
using Serilog;
using System.Diagnostics;

namespace CameraPro.Recording;

public class VideoRecorder : IVideoRecorder, IDisposable
{
    private readonly EncoderManager _encoderManager;
    private readonly RecordingStateMachine _stateMachine;
    private readonly ILogger _logger;

    private Process? _ffmpegProcess;
    private StreamWriter? _ffmpegInput;
    private string _currentFilePath = string.Empty;
    private CaptureSettings _currentSettings = new();
    private DateTime _recordingStartTime;
    private long _bytesWritten;
    private bool _disposed;

    public event EventHandler<RecordingStatus>? StatusChanged;
    public event EventHandler<TimeSpan>? DurationUpdated;
    public event EventHandler<long>? FileSizeUpdated;
    public event EventHandler<RecordingSession>? RecordingComplete;

    public bool IsRecording => _stateMachine.IsRecording;
    public bool IsPaused => _stateMachine.IsPaused;
    public RecordingStatus Status => _stateMachine.CurrentState switch
    {
        RecordingState.Idle => RecordingStatus.Idle,
        RecordingState.Recording => RecordingStatus.Recording,
        RecordingState.Paused => RecordingStatus.Paused,
        RecordingState.Encoding => RecordingStatus.Encoding,
        _ => RecordingStatus.Idle
    };

    public VideoRecorder()
    {
        _encoderManager = new EncoderManager();
        _stateMachine = new RecordingStateMachine();
        _logger = Log.ForContext<VideoRecorder>();

        _stateMachine.StateChanged += OnStateChanged;
    }

    private void OnStateChanged(object? sender, RecordingState state)
    {
        var status = state switch
        {
            RecordingState.Idle => RecordingStatus.Idle,
            RecordingState.Recording => RecordingStatus.Recording,
            RecordingState.Paused => RecordingStatus.Paused,
            RecordingState.Encoding => RecordingStatus.Encoding,
            _ => RecordingStatus.Idle
        };
        StatusChanged?.Invoke(this, status);
    }

    public void StartRecording(string path, CaptureSettings settings)
    {
        if (!_stateMachine.Transition(RecordingEvent.Start))
        {
            throw new InvalidOperationException("Cannot start recording from current state");
        }

        _currentFilePath = path;
        _currentSettings = settings;
        _recordingStartTime = DateTime.Now;
        _bytesWritten = 0;

        try
        {
            var encoderSettings = _encoderManager.GetSettings(QualityPreset.High);
            var ffmpegArgs = _encoderManager.BuildFFmpegArgs(
                encoderSettings,
                settings.Resolution.Width,
                settings.Resolution.Height,
                settings.FrameRate,
                path
            );

            _ffmpegProcess = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "ffmpeg",
                    Arguments = ffmpegArgs,
                    UseShellExecute = false,
                    RedirectStandardInput = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                }
            };

            _ffmpegProcess.Start();
            _ffmpegInput = _ffmpegProcess.StandardInput;

            if (!_stateMachine.Transition(RecordingEvent.Start))
            {
                throw new InvalidOperationException("Failed to transition to Recording state");
            }

            _logger.Information("Recording started: {Path}", path);

            Task.Run(MonitorRecording);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to start recording");
            _stateMachine.Transition(RecordingEvent.Error);
            throw;
        }
    }

    public RecordingSession StopRecording()
    {
        if (!IsRecording && !IsPaused)
        {
            throw new InvalidOperationException("Not currently recording");
        }

        _stateMachine.Transition(RecordingEvent.Stop);

        try
        {
            _ffmpegInput?.Write('q');
            _ffmpegInput?.Flush();

            if (_ffmpegProcess != null && !_ffmpegProcess.HasExited)
            {
                _ffmpegProcess.WaitForExit(10000);
            }
        }
        catch (Exception ex)
        {
            _logger.Warning(ex, "Error during recording stop");
        }
        finally
        {
            CleanupFFmpeg();
            _stateMachine.Transition(RecordingEvent.EncodeComplete);
        }

        var session = new RecordingSession
        {
            Id = Guid.NewGuid(),
            StartTime = _recordingStartTime,
            Duration = DateTime.Now - _recordingStartTime,
            FilePath = _currentFilePath,
            FileSizeBytes = GetFileSize(_currentFilePath)
        };

        _logger.Information("Recording stopped: {Path}, Duration: {Duration}", _currentFilePath, session.Duration);
        RecordingComplete?.Invoke(this, session);

        return session;
    }

    public void PauseRecording()
    {
        if (!_stateMachine.Transition(RecordingEvent.Pause))
        {
            throw new InvalidOperationException("Cannot pause from current state");
        }
        _logger.Information("Recording paused");
    }

    public void ResumeRecording()
    {
        if (!_stateMachine.Transition(RecordingEvent.Resume))
        {
            throw new InvalidOperationException("Cannot resume from current state");
        }
        _logger.Information("Recording resumed");
    }

    public void WriteFrame(byte[] frameData)
    {
        if (!IsRecording || _ffmpegInput == null)
            return;

        try
        {
            _ffmpegInput.BaseStream.Write(frameData, 0, frameData.Length);
            _bytesWritten += frameData.Length;
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error writing frame");
        }
    }

    private async Task MonitorRecording()
    {
        var timer = new System.Timers.Timer(1000);
        timer.Elapsed += (s, e) =>
        {
            if (IsRecording)
            {
                var duration = DateTime.Now - _recordingStartTime;
                DurationUpdated?.Invoke(this, duration);
                FileSizeUpdated?.Invoke(this, _bytesWritten);
            }
        };
        timer.Start();

        while (IsRecording || IsPaused)
        {
            await Task.Delay(100);
        }

        timer.Stop();
        timer.Dispose();
    }

    private long GetFileSize(string path)
    {
        try
        {
            return new FileInfo(path).Length;
        }
        catch
        {
            return 0;
        }
    }

    private void CleanupFFmpeg()
    {
        try
        {
            _ffmpegInput?.Dispose();
            _ffmpegProcess?.Dispose();
            _ffmpegInput = null;
            _ffmpegProcess = null;
        }
        catch { }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        if (IsRecording)
        {
            StopRecording();
        }

        CleanupFFmpeg();
        GC.SuppressFinalize(this);
    }
}