using System.Windows.Media.Imaging;
using CameraPro.Core.Models;
using Serilog;

namespace CameraPro.Camera;

public class PreviewPipeline : IDisposable
{
    private readonly object _frameLock = new();
    private BitmapSource? _latestFrame;
    private readonly int _targetFps = 30;
    private readonly int _frameDelayMs;
    private CancellationTokenSource? _captureCts;
    private Task? _captureTask;
    private bool _isRunning;
    private readonly Queue<BitmapSource> _frameBuffer = new();
    private readonly int _maxBufferSize = 2;

    public event EventHandler<BitmapSource>? FrameReady;

    public PreviewPipeline()
    {
        _frameDelayMs = 1000 / _targetFps;
    }

    public void Start(Func<Task<BitmapSource?>> frameCaptureFunc)
    {
        if (_isRunning)
            return;

        _captureCts = new CancellationTokenSource();
        _isRunning = true;
        _captureTask = RunCaptureLoopAsync(frameCaptureFunc, _captureCts.Token);
        Log.Information("PreviewPipeline started at {Fps} fps", _targetFps);
    }

    public void Stop()
    {
        if (!_isRunning)
            return;

        _captureCts?.Cancel();
        _captureCts?.Dispose();
        _captureCts = null;
        _isRunning = false;

        lock (_frameLock)
        {
            _frameBuffer.Clear();
        }

        Log.Information("PreviewPipeline stopped");
    }

    public BitmapSource? GetLatestFrame()
    {
        lock (_frameLock)
        {
            return _latestFrame;
        }
    }

    private async Task RunCaptureLoopAsync(Func<Task<BitmapSource?>> captureFunc, CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            try
            {
                var frame = await captureFunc();
                if (frame != null)
                {
                    UpdateLatestFrame(frame);
                    FrameReady?.Invoke(this, frame);
                }
                await Task.Delay(_frameDelayMs, ct);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Error in preview pipeline capture");
            }
        }
    }

    private void UpdateLatestFrame(BitmapSource frame)
    {
        lock (_frameLock)
        {
            _latestFrame = frame;

            if (_frameBuffer.Count >= _maxBufferSize)
            {
                _frameBuffer.Dequeue();
            }
            _frameBuffer.Enqueue(frame);
        }
    }

    public void Dispose()
    {
        Stop();
    }
}
