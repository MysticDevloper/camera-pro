using System.Windows.Media.Imaging;
using Serilog;

namespace CameraPro.Capture;

public class BurstMode
{
    private readonly PhotoCapture _photoCapture;
    private CancellationTokenSource? _cts;
    private bool _isCancelled;

    public event EventHandler<int>? ProgressChanged;
    public event EventHandler<string>? PhotoSaved;
    public event EventHandler? BurstCompleted;
    public event EventHandler? BurstCancelled;

    public BurstMode(PhotoCapture photoCapture)
    {
        _photoCapture = photoCapture;
    }

    public async Task<List<string>> CaptureAsync(int count, CancellationToken? externalToken = null)
    {
        _cts = new CancellationTokenSource();
        var linkedToken = externalToken.HasValue
            ? CancellationTokenSource.CreateLinkedTokenSource(_cts.Token, externalToken.Value).Token
            : _cts.Token;

        _isCancelled = false;
        var results = new List<string>();

        try
        {
            for (int i = 0; i < count && !linkedToken.IsCancellationRequested; i++)
            {
                var frame = _photoCapture.GetCurrentFrameForBurst();
                if (frame == null)
                {
                    Log.Warning("No frame available for burst capture at index {Index}", i);
                    continue;
                }

                var filePath = _photoCapture.GenerateFilePathForBurst();
                var saved = await _photoCapture.SaveFrameForBurstAsync(frame, filePath, linkedToken);
                
                if (!string.IsNullOrEmpty(saved))
                {
                    results.Add(saved);
                    PhotoSaved?.Invoke(this, saved);
                }

                ProgressChanged?.Invoke(this, i + 1);
                
                if (i < count - 1)
                {
                    await Task.Delay(_photoCapture.GetBurstInterval(), linkedToken);
                }
            }

            if (linkedToken.IsCancellationRequested)
            {
                _isCancelled = true;
                BurstCancelled?.Invoke(this, EventArgs.Empty);
            }
            else
            {
                BurstCompleted?.Invoke(this, EventArgs.Empty);
            }

            Log.Information("Burst mode completed: {Count} photos", results.Count);
        }
        catch (OperationCanceledException)
        {
            _isCancelled = true;
            BurstCancelled?.Invoke(this, EventArgs.Empty);
            Log.Information("Burst mode cancelled");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Burst mode error");
        }

        return results;
    }

    public void Cancel()
    {
        _cts?.Cancel();
        _isCancelled = true;
    }

    public bool IsCancelled => _isCancelled;
}

public static class BurstCount
{
    public const int Three = 3;
    public const int Five = 5;
    public const int Ten = 10;
    public const int Continuous = -1;
}
