using CameraPro.Core.Enums;
using CameraPro.Core.Models;

namespace CameraPro.Core.Interfaces;

public interface IVideoRecorder
{
    void StartRecording(string path, CaptureSettings settings);
    RecordingSession StopRecording();
    void PauseRecording();
    void ResumeRecording();
    void WriteFrame(byte[] frameData);
    bool IsRecording { get; }
    bool IsPaused { get; }
    RecordingStatus Status { get; }
    
    event EventHandler<RecordingStatus>? StatusChanged;
    event EventHandler<TimeSpan>? DurationUpdated;
    event EventHandler<long>? FileSizeUpdated;
    event EventHandler<RecordingSession>? RecordingComplete;
}