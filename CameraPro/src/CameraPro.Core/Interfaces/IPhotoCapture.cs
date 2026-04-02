namespace CameraPro.Core.Interfaces;

public interface IPhotoCapture
{
    string CapturePhoto();
    Task<string> CapturePhotoAsync(CancellationToken cancellationToken = default);
    List<string> CaptureBurst(int count);
    Task<List<string>> CaptureBurstAsync(int count, CancellationToken cancellationToken = default);
    string CaptureWithDelay(int seconds);
    Task<string> CaptureWithDelayAsync(int seconds, CancellationToken cancellationToken = default);
    
    event EventHandler<string>? PhotoCaptured;
    event EventHandler<int>? BurstProgress;
    event EventHandler<int>? TimerCountdown;
    event EventHandler<string>? CaptureError;
}
