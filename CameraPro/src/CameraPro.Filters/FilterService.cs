using OpenCvSharp;
using CameraPro.Core.Interfaces;
using CameraPro.Core.Models;
using CameraPro.Core.Enums;
using System.Collections.Concurrent;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Runtime.InteropServices;

namespace CameraPro.Filters;

public class FilterService : IDisposable
{
    private readonly FilterPipeline _pipeline;
    private readonly FilterPresets _presets;
    private readonly FaceDetector _faceDetector;
    private readonly TextOverlay _textOverlay;
    private readonly TimestampOverlay _timestampOverlay;
    private readonly LogoOverlay _logoOverlay;

    private readonly ConcurrentQueue<Mat> _inputQueue;
    private readonly ConcurrentQueue<Mat> _outputQueue;
    private CancellationTokenSource? _cancellationTokenSource;
    private Task? _processingTask;
    private bool _isProcessing;
    private int _targetFps = 30;
    private int _skipFramesOnLag = 0;
    private DateTime _lastFrameTime = DateTime.MinValue;

    public bool IsEnabled { get; set; } = true;
    public int TargetFps
    {
        get => _targetFps;
        set => _targetFps = Math.Max(1, Math.Min(60, value));
    }

    public FilterPipeline Pipeline => _pipeline;
    public FilterPresets Presets => _presets;
    public FaceDetector FaceDetector => _faceDetector;
    public TextOverlay TextOverlay => _textOverlay;
    public TimestampOverlay TimestampOverlay => _timestampOverlay;
    public LogoOverlay LogoOverlay => _logoOverlay;

    public event EventHandler<FilterAppliedEventArgs>? FilterApplied;
    public event EventHandler<double>? FpsUpdated;

    public FilterService()
    {
        _pipeline = new FilterPipeline();
        _presets = new FilterPresets();
        _faceDetector = new FaceDetector();
        _textOverlay = new TextOverlay();
        _timestampOverlay = new TimestampOverlay();
        _logoOverlay = new LogoOverlay();
        
        _inputQueue = new ConcurrentQueue<Mat>();
        _outputQueue = new ConcurrentQueue<Mat>();
    }

    public void StartAsyncProcessing()
    {
        if (_isProcessing)
            return;

        _cancellationTokenSource = new CancellationTokenSource();
        _isProcessing = true;

        _processingTask = Task.Run(() => ProcessFramesAsync(_cancellationTokenSource.Token));
    }

    public void StopAsyncProcessing()
    {
        _cancellationTokenSource?.Cancel();
        _isProcessing = false;
    }

    private async Task ProcessFramesAsync(CancellationToken cancellationToken)
    {
        var frameTime = TimeSpan.FromMilliseconds(1000.0 / _targetFps);

        while (!cancellationToken.IsCancellationRequested)
        {
            var frameStart = DateTime.UtcNow;

            if (_inputQueue.TryDequeue(out var inputFrame))
            {
                try
                {
                    var outputFrame = ProcessFrame(inputFrame);
                    _outputQueue.Enqueue(outputFrame);
                    inputFrame.Dispose();
                }
                catch (Exception ex)
                {
                    Serilog.Log.Error(ex, "Error processing frame");
                }
            }

            var elapsed = DateTime.UtcNow - frameStart;
            if (elapsed < frameTime)
            {
                await Task.Delay(frameTime - elapsed, cancellationToken);
            }

            if (_skipFramesOnLag > 0 && _inputQueue.Count > _skipFramesOnLag)
            {
                while (_inputQueue.Count > 1 && _inputQueue.TryDequeue(out var dropped))
                {
                    dropped.Dispose();
                }
            }

            var fps = frameTime.TotalSeconds / (DateTime.UtcNow - frameStart).TotalSeconds;
            FpsUpdated?.Invoke(this, fps);
        }
    }

    public void QueueFrame(Mat frame)
    {
        if (!_isProcessing || !IsEnabled)
            return;

        var cloned = frame.Clone();
        _inputQueue.Enqueue(cloned);
    }

    public Mat? GetProcessedFrame()
    {
        if (_outputQueue.TryDequeue(out var frame))
            return frame;
        return null;
    }

    public Mat ProcessFrame(Mat input)
    {
        if (input.Empty())
            return input;

        var result = input.Clone();

        if (IsEnabled)
        {
            result = _pipeline.ProcessFrame(result);
        }

        if (_faceDetector.IsEnabled)
        {
            result = _faceDetector.DrawFaceRectangles(result);
        }

        if (_timestampOverlay.IsEnabled)
        {
            _timestampOverlay.UpdateTimestamp();
            result = _timestampOverlay.Apply(result);
        }
        else if (_textOverlay.IsEnabled)
        {
            result = _textOverlay.Apply(result);
        }

        if (_logoOverlay.IsEnabled)
        {
            result = _logoOverlay.Apply(result);
        }

        _lastFrameTime = DateTime.UtcNow;
        FilterApplied?.Invoke(this, new FilterAppliedEventArgs { FrameProcessed = true });

        return result;
    }

    public Mat ProcessFrameWithSettings(Mat input, FilterEffect effect)
    {
        if (input.Empty())
            return input;

        var result = input.Clone();

        if (effect.IsEnabled)
        {
            foreach (var param in effect.Parameters)
            {
                var filter = CreateFilterFromEffect(effect, param.Key, param.Value);
                if (filter != null)
                {
                    result = filter.Apply(result);
                }
            }
        }

        return result;
    }

    private IFilter? CreateFilterFromEffect(FilterEffect effect, string paramKey, double paramValue)
    {
        return paramKey switch
        {
            "Brightness" or "Contrast" or "Saturation" or "Hue" or "Gamma" => new Adjustments
            {
                Parameters = effect.Parameters
            },
            _ => effect.Name switch
            {
                "Grayscale" => new ColorFilters { Type = FilterType.Grayscale, Intensity = paramValue },
                "Sepia" => new ColorFilters { Type = FilterType.Sepia, Intensity = paramValue },
                "Warm" => new ColorFilters { Type = FilterType.Warm, Intensity = paramValue },
                "Cool" => new ColorFilters { Type = FilterType.Cool, Intensity = paramValue },
                "Blur" => new EffectsProcessor { EffectType = FilterType.Blur, Intensity = paramValue },
                "Sharpen" => new EffectsProcessor { EffectType = FilterType.Sharpen, Intensity = paramValue },
                "Vignette" => new EffectsProcessor { EffectType = FilterType.Vignette, Intensity = paramValue },
                _ => null
            }
        };
    }

    public void ApplyPreset(string presetName)
    {
        var preset = _presets.GetPreset(presetName);
        if (preset == null)
            return;

        _pipeline.Clear();

        foreach (var config in preset.Filters)
        {
            var filter = CreateFilterFromConfig(config);
            if (filter != null)
            {
                _pipeline.AddFilter(filter);
            }
        }
    }

    private IFilter? CreateFilterFromConfig(FilterConfig config)
    {
        return config.Type switch
        {
            "Adjustments" => new Adjustments { Parameters = config.Parameters },
            "ColorFilters" => new ColorFilters
            {
                Type = (FilterType)config.Parameters.GetValueOrDefault("Type", 0),
                Intensity = config.Parameters.GetValueOrDefault("Intensity", 1.0),
                Parameters = config.Parameters
            },
            "EffectsProcessor" => new EffectsProcessor
            {
                EffectType = (FilterType)config.Parameters.GetValueOrDefault("EffectType", 0),
                Intensity = config.Parameters.GetValueOrDefault("Intensity", 1.0),
                Parameters = config.Parameters
            },
            _ => null
        };
    }

    public void SaveCurrentAsPreset(string name, string description)
    {
        var preset = new PresetData
        {
            Name = name,
            Description = description,
            Filters = new List<FilterConfig>()
        };

        foreach (var filter in _pipeline.Filters)
        {
            var config = new FilterConfig
            {
                Type = filter.GetType().Name,
                Parameters = filter.Parameters
            };
            preset.Filters.Add(config);
        }

        _presets.SaveCustomPreset(name, preset);
    }

    public void Reset()
    {
        _pipeline.Clear();
        _faceDetector.IsEnabled = false;
        _textOverlay.IsEnabled = false;
        _timestampOverlay.IsEnabled = false;
        _logoOverlay.IsEnabled = false;
    }

    public BitmapSource? GetFilteredBitmapSource(Mat input)
    {
        if (input.Empty())
            return null;

        var processed = ProcessFrame(input);
        var bitmap = MatToBitmapSource(processed);
        processed.Dispose();

        return bitmap;
    }

    private BitmapSource MatToBitmapSource(Mat mat)
    {
        if (mat.Empty())
            throw new ArgumentException("Input mat is empty");

        var converted = new Mat();
        if (mat.Type() == MatType.CV_8UC1)
        {
            Cv2.CvtColor(mat, converted, ColorConversionCodes.GRAY2BGRA);
        }
        else if (mat.Type() == MatType.CV_8UC3)
        {
            Cv2.CvtColor(mat, converted, ColorConversionCodes.BGR2BGRA);
        }
        else if (mat.Type() == MatType.CV_8UC4)
        {
            mat.CopyTo(converted);
        }
        else
        {
            mat.ConvertTo(converted, MatType.CV_8UC4);
        }

        var width = converted.Width;
        var height = converted.Height;
        var stride = width * 4;
        var byteCount = stride * height;

        var bytes = new byte[byteCount];
        Marshal.Copy(converted.Data, bytes, 0, byteCount);

        var bitmap = BitmapSource.Create(
            width, height,
            96, 96,
            PixelFormats.Bgra32,
            null,
            bytes,
            stride);

        bitmap.Freeze();
        converted.Dispose();

        return bitmap;
    }

    public BitmapSource? ProcessAndConvertToBitmapSource(BitmapSource input)
    {
        try
        {
            var mat = BitmapSourceToMat(input);
            var processed = ProcessFrame(mat);
            var result = MatToBitmapSource(processed);
            mat.Dispose();
            processed.Dispose();
            return result;
        }
        catch (Exception ex)
        {
            Serilog.Log.Error(ex, "Failed to process frame for display");
            return input;
        }
    }

    private Mat BitmapSourceToMat(BitmapSource source)
    {
        var writeable = new WriteableBitmap(source);
        int width = writeable.PixelWidth;
        int height = writeable.PixelHeight;
        int stride = width * 4;
        byte[] pixels = new byte[height * stride];
        writeable.CopyPixels(pixels, stride, 0);

        var mat = new Mat(height, width, MatType.CV_8UC4, pixels);
        Cv2.CvtColor(mat, mat, ColorConversionCodes.BGRA2BGR);
        return mat;
    }

    private bool _useGpuAcceleration = false;

    public bool UseGpuAcceleration
    {
        get => _useGpuAcceleration && HasGpuSupport();
        set => _useGpuAcceleration = value && HasGpuSupport();
    }

    public bool HasGpuSupport()
    {
        try
        {
            using var gpu = new GpuMat();
            return !gpu.Empty();
        }
        catch
        {
            return false;
        }
    }

    public Mat ProcessFrameWithGpu(Mat input)
    {
        if (!UseGpuAcceleration || input.Empty())
            return ProcessFrame(input);

        try
        {
            using var gpuInput = new GpuMat(input);
            var result = new Mat();
            gpuInput.Download(result);
            return ProcessFrame(result);
        }
        catch (Exception ex)
        {
            Serilog.Log.Warning(ex, "GPU processing failed, falling back to CPU");
            return ProcessFrame(input);
        }
    }

    public void Dispose()
    {
        StopAsyncProcessing();
        _faceDetector.Dispose();

        while (_inputQueue.TryDequeue(out var frame))
            frame.Dispose();
        while (_outputQueue.TryDequeue(out var frame))
            frame.Dispose();
    }
}

public class FilterAppliedEventArgs : EventArgs
{
    public bool FrameProcessed { get; set; }
}

public interface IFilterService
{
    bool IsEnabled { get; set; }
    int TargetFps { get; set; }
    bool UseGpuAcceleration { get; set; }
    bool HasGpuSupport();
    FilterPipeline Pipeline { get; }
    FilterPresets Presets { get; }
    FaceDetector FaceDetector { get; }
    Mat ProcessFrame(Mat input);
    Mat ProcessFrameWithGpu(Mat input);
    void QueueFrame(Mat frame);
    Mat? GetProcessedFrame();
    BitmapSource? GetFilteredBitmapSource(Mat input);
    BitmapSource? ProcessAndConvertToBitmapSource(BitmapSource input);
    void ApplyPreset(string presetName);
    Reset();
}