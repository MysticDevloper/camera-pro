namespace CameraPro.Filters;

using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.IO;
using System.Windows;

public class FilterPipeline
{
    private readonly List<IFilter> _filters = new();
    private readonly object _lock = new();
    private bool _hasFilters;

    public bool HasFilters => _hasFilters;

    public IReadOnlyList<IFilter> Filters
    {
        get
        {
            lock (_lock)
            {
                return _filters.ToList().AsReadOnly();
            }
        }
    }

    public void AddFilter(FilterType type, double intensity = 1.0)
    {
        lock (_lock)
        {
            var filter = CreateFilter(type, intensity);
            if (filter != null)
            {
                _filters.Add(filter);
                _hasFilters = _filters.Count > 0;
            }
        }
    }

    private IFilter? CreateFilter(FilterType type, double intensity)
    {
        return type switch
        {
            FilterType.Grayscale => new GrayscaleFilter { Intensity = intensity },
            FilterType.Sepia => new SepiaFilter { Intensity = intensity },
            FilterType.Negative => new NegativeFilter { Intensity = intensity },
            FilterType.Warm => new WarmFilter { Intensity = intensity },
            FilterType.Cool => new CoolFilter { Intensity = intensity },
            FilterType.Blur => new BlurFilter { Intensity = intensity },
            FilterType.Sharpen => new SharpenFilter { Intensity = intensity },
            FilterType.Vignette => new VignetteFilter { Intensity = intensity },
            _ => null
        };
    }

    public void AddFilter(IFilter filter)
    {
        lock (_lock)
        {
            _filters.Add(filter);
            _hasFilters = _filters.Count > 0;
        }
    }

    public void RemoveFilter(IFilter filter)
    {
        lock (_lock)
        {
            _filters.Remove(filter);
            _hasFilters = _filters.Count > 0;
        }
    }

    public void RemoveFilter(int index)
    {
        lock (_lock)
        {
            if (index >= 0 && index < _filters.Count)
            {
                _filters.RemoveAt(index);
                _hasFilters = _filters.Count > 0;
            }
        }
    }

    public void Clear()
    {
        lock (_lock)
        {
            _filters.Clear();
            _hasFilters = false;
        }
    }

    public void Reorder(int oldIndex, int newIndex)
    {
        lock (_lock)
        {
            if (oldIndex >= 0 && oldIndex < _filters.Count &&
                newIndex >= 0 && newIndex < _filters.Count)
            {
                var item = _filters[oldIndex];
                _filters.RemoveAt(oldIndex);
                _filters.Insert(newIndex, item);
            }
        }
    }

    public BitmapSource ProcessFrame(BitmapSource input)
    {
        if (!_hasFilters || input == null)
            return input;

        try
        {
            lock (_lock)
            {
                foreach (var filter in _filters)
                {
                    if (filter.IsEnabled)
                    {
                        input = ApplyFilterToBitmap(input, filter);
                    }
                }
            }
        }
        catch
        {
            return input;
        }

        return input;
    }

    private BitmapSource ApplyFilterToBitmap(BitmapSource source, IFilter filter)
    {
        try
        {
            var width = source.PixelWidth;
            var height = source.PixelHeight;
            var stride = width * 4;
            var pixels = new byte[height * stride];

            source.CopyPixels(pixels, stride, 0);

            for (int i = 0; i < pixels.Length; i += 4)
            {
                var b = pixels[i];
                var g = pixels[i + 1];
                var r = pixels[i + 2];

                var adjusted = filter switch
                {
                    GrayscaleFilter => ApplyGrayscale(r, g, b, filter.Intensity),
                    SepiaFilter => ApplySepia(r, g, b, filter.Intensity),
                    NegativeFilter => ApplyNegative(r, g, b, filter.Intensity),
                    WarmFilter => ApplyWarm(r, g, b, filter.Intensity),
                    CoolFilter => ApplyCool(r, g, b, filter.Intensity),
                    BlurFilter => ApplyBlur(r, g, b, filter.Intensity),
                    SharpenFilter => ApplySharpen(r, g, b, filter.Intensity),
                    VignetteFilter => ApplyVignette(r, g, b, filter.Intensity, width, height, i),
                    _ => (r, g, b)
                };

                pixels[i] = adjusted.b;
                pixels[i + 1] = adjusted.g;
                pixels[i + 2] = adjusted.r;
            }

            var result = BitmapSource.Create(width, height, source.DpiX, source.DpiY,
                PixelFormats.Bgra32, null, pixels, stride);
            result.Freeze();
            return result;
        }
        catch
        {
            return source;
        }
    }

    private (byte r, byte g, byte b) ApplyGrayscale(byte r, byte g, byte b, double intensity)
    {
        var gray = (byte)(0.299 * r + 0.587 * g + 0.114 * b);
        return (
            (byte)Math.Clamp(r + (gray - r) * intensity, 0, 255),
            (byte)Math.Clamp(g + (gray - g) * intensity, 0, 255),
            (byte)Math.Clamp(b + (gray - b) * intensity, 0, 255)
        );
    }

    private (byte r, byte g, byte b) ApplySepia(byte r, byte g, byte b, double intensity)
    {
        var tr = Math.Min(255, 0.393 * r + 0.769 * g + 0.189 * b);
        var tg = Math.Min(255, 0.349 * r + 0.686 * g + 0.168 * b);
        var tb = Math.Min(255, 0.272 * r + 0.534 * g + 0.131 * b);
        return (
            (byte)Math.Clamp(r + (tr - r) * intensity, 0, 255),
            (byte)Math.Clamp(g + (tg - g) * intensity, 0, 255),
            (byte)Math.Clamp(b + (tb - b) * intensity, 0, 255)
        );
    }

    private (byte r, byte g, byte b) ApplyNegative(byte r, byte g, byte b, double intensity)
    {
        return (
            (byte)Math.Clamp(r + (255 - 2 * r) * intensity, 0, 255),
            (byte)Math.Clamp(g + (255 - 2 * g) * intensity, 0, 255),
            (byte)Math.Clamp(b + (255 - 2 * b) * intensity, 0, 255)
        );
    }

    private (byte r, byte g, byte b) ApplyWarm(byte r, byte g, byte b, double intensity)
    {
        return (
            (byte)Math.Clamp(r + 30 * intensity, 0, 255),
            (byte)Math.Clamp(g + 10 * intensity, 0, 255),
            (byte)Math.Clamp(b - 20 * intensity, 0, 255)
        );
    }

    private (byte r, byte g, byte b) ApplyCool(byte r, byte g, byte b, double intensity)
    {
        return (
            (byte)Math.Clamp(r - 20 * intensity, 0, 255),
            (byte)Math.Clamp(g + 10 * intensity, 0, 255),
            (byte)Math.Clamp(b + 30 * intensity, 0, 255)
        );
    }

    private (byte r, byte g, byte b) ApplyBlur(byte r, byte g, byte b, double intensity)
    {
        return (r, g, b);
    }

    private (byte r, byte g, byte b) ApplySharpen(byte r, byte g, byte b, double intensity)
    {
        return (r, g, b);
    }

    private (byte r, byte g, byte b) ApplyVignette(byte r, byte g, byte b, double intensity, int width, int height, int index)
    {
        var x = (index / 4) % width;
        var y = (index / 4) / width;
        var cx = width / 2.0;
        var cy = height / 2.0;
        var dx = (x - cx) / cx;
        var dy = (y - cy) / cy;
        var dist = Math.Sqrt(dx * dx + dy * dy);
        var factor = Math.Max(0, 1 - dist * intensity);
        return (
            (byte)(r * factor),
            (byte)(g * factor),
            (byte)(b * factor)
        );
    }

    public List<FilterEffect> GetFilters()
    {
        lock (_lock)
        {
            return _filters.Select(f => new FilterEffect
            {
                Name = f.Name,
                IsEnabled = f.IsEnabled,
                Parameters = f.Parameters
            }).ToList();
        }
    }

    public OpenCvSharp.Mat ProcessOpenCvFrame(OpenCvSharp.Mat input)
    {
        if (input.Empty())
            return input;

        OpenCvSharp.Mat result = input.Clone();

        lock (_lock)
        {
            foreach (var filter in _filters)
            {
                if (filter.IsEnabled)
                {
                    result = filter.Apply(result);
                }
            }
        }

        return result;
    }
}

public class GrayscaleFilter : IFilter
{
    public string Name => "Grayscale";
    public bool IsEnabled { get; set; } = true;
    public Dictionary<string, double> Parameters { get; set; } = new();
    public double Intensity { get; set; } = 1.0;
    public OpenCvSharp.Mat Apply(OpenCvSharp.Mat input) => input.CvtColor(OpenCvSharp.ColorConversionCodes.BGR2GRAY);
}

public class SepiaFilter : IFilter
{
    public string Name => "Sepia";
    public bool IsEnabled { get; set; } = true;
    public Dictionary<string, double> Parameters { get; set; } = new();
    public double Intensity { get; set; } = 1.0;
    public OpenCvSharp.Mat Apply(OpenCvSharp.Mat input)
    {
        var kernel = new OpenCvSharp.Mat(3, 3, OpenCvSharp.MatType.CV_32F, new float[] { 0.272f, 0.534f, 0.131f, 0.349f, 0.686f, 0.168f, 0.393f, 0.769f, 0.189f });
        return input.Filter2D(-1, kernel);
    }
}

public class NegativeFilter : IFilter
{
    public string Name => "Negative";
    public bool IsEnabled { get; set; } = true;
    public Dictionary<string, double> Parameters { get; set; } = new();
    public double Intensity { get; set; } = 1.0;
    public OpenCvSharp.Mat Apply(OpenCvSharp.Mat input) => new OpenCvSharp.Mat(input.Size(), input.Type()) - input;
}

public class WarmFilter : IFilter
{
    public string Name => "Warm";
    public bool IsEnabled { get; set; } = true;
    public Dictionary<string, double> Parameters { get; set; } = new();
    public double Intensity { get; set; } = 1.0;
    public OpenCvSharp.Mat Apply(OpenCvSharp.Mat input) => input;
}

public class CoolFilter : IFilter
{
    public string Name => "Cool";
    public bool IsEnabled { get; set; } = true;
    public Dictionary<string, double> Parameters { get; set; } = new();
    public double Intensity { get; set; } = 1.0;
    public OpenCvSharp.Mat Apply(OpenCvSharp.Mat input) => input;
}

public class BlurFilter : IFilter
{
    public string Name => "Blur";
    public bool IsEnabled { get; set; } = true;
    public Dictionary<string, double> Parameters { get; set; } = new();
    public double Intensity { get; set; } = 1.0;
    public OpenCvSharp.Mat Apply(OpenCvSharp.Mat input) => input.GaussianBlur(new OpenCvSharp.Size(5, 5), 0);
}

public class SharpenFilter : IFilter
{
    public string Name => "Sharpen";
    public bool IsEnabled { get; set; } = true;
    public Dictionary<string, double> Parameters { get; set; } = new();
    public double Intensity { get; set; } = 1.0;
    public OpenCvSharp.Mat Apply(OpenCvSharp.Mat input)
    {
        var kernel = new OpenCvSharp.Mat(3, 3, OpenCvSharp.MatType.CV_32F, new float[] { 0, -1, 0, -1, 5, -1, 0, -1, 0 });
        return input.Filter2D(-1, kernel);
    }
}

public class VignetteFilter : IFilter
{
    public string Name => "Vignette";
    public bool IsEnabled { get; set; } = true;
    public Dictionary<string, double> Parameters { get; set; } = new();
    public double Intensity { get; set; } = 1.0;
    public OpenCvSharp.Mat Apply(OpenCvSharp.Mat input)
    {
        var result = input.Clone();
        var mask = OpenCvSharp.Mat.Zeros(input.Size(), OpenCvSharp.MatType.CV_32F);
        var kernel = new OpenCvSharp.Mat(1, 3, OpenCvSharp.MatType.CV_32F, new float[] { 0, 1, 0 });
        OpenCvSharp.Cv2.Filter2D(input, mask, -1, kernel);
        return result;
    }
}

public interface IFilter
{
    string Name { get; }
    bool IsEnabled { get; set; }
    Dictionary<string, double> Parameters { get; set; }
    OpenCvSharp.Mat Apply(OpenCvSharp.Mat input);
}