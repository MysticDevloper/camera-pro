using System.IO;
using System.Windows.Media.Imaging;
using System.Media;
using Serilog;

namespace CameraPro.Capture;

public class TimerMode
{
    private readonly PhotoCapture _photoCapture;
    private CancellationTokenSource? _cts;
    private bool _isCancelled;

    public event EventHandler<int>? CountdownTick;
    public event EventHandler? TimerCompleted;
    public event EventHandler? TimerCancelled;

    public TimerMode(PhotoCapture photoCapture)
    {
        _photoCapture = photoCapture;
    }

    public async Task<string> CaptureWithTimerAsync(int seconds, CancellationToken? externalToken = null)
    {
        _cts = new CancellationTokenSource();
        var linkedToken = externalToken.HasValue
            ? CancellationTokenSource.CreateLinkedTokenSource(_cts.Token, externalToken.Value).Token
            : _cts.Token;

        _isCancelled = false;

        try
        {
            for (int i = seconds; i > 0 && !linkedToken.IsCancellationRequested; i--)
            {
                CountdownTick?.Invoke(this, i);
                PlayBeepIfEnabled(i);
                await Task.Delay(1000, linkedToken);
            }

            if (linkedToken.IsCancellationRequested)
            {
                _isCancelled = true;
                TimerCancelled?.Invoke(this, EventArgs.Empty);
                return string.Empty;
            }

            TimerCompleted?.Invoke(this, EventArgs.Empty);
            return _photoCapture.CapturePhoto();
        }
        catch (OperationCanceledException)
        {
            _isCancelled = true;
            TimerCancelled?.Invoke(this, EventArgs.Empty);
            Log.Information("Timer mode cancelled");
            return string.Empty;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Timer mode error");
            return string.Empty;
        }
    }

    public async Task ShowCountdownOnPreviewAsync(int seconds)
    {
        for (int i = seconds; i > 0; i--)
        {
            CountdownTick?.Invoke(this, i);
            await Task.Delay(1000);
        }
    }

    private void PlayBeepIfEnabled(int secondsRemaining)
    {
        try
        {
            if (secondsRemaining <= 3)
            {
                SystemSounds.Asterisk.Play();
            }
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Failed to play beep sound");
        }
    }

    public void Cancel()
    {
        _cts?.Cancel();
        _isCancelled = true;
    }

    public bool IsCancelled => _isCancelled;
}

public static class TimerDuration
{
    public const int ThreeSeconds = 3;
    public const int FiveSeconds = 5;
    public const int TenSeconds = 10;
}
