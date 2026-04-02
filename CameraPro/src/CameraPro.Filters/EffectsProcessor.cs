using OpenCvSharp;
using CameraPro.Core.Enums;

namespace CameraPro.Filters;

public class EffectsProcessor : IFilter
{
    public string Name => EffectType.ToString();
    public bool IsEnabled { get; set; } = true;
    public Dictionary<string, double> Parameters { get; set; } = new();
    public FilterType EffectType { get; set; }
    public double Intensity { get; set; } = 1.0;

    public Mat Apply(Mat input)
    {
        if (!IsEnabled || input.Empty()) return input;

        Mat output = input.Clone();

        switch (EffectType)
        {
            case FilterType.Blur:
                int blurSize = (int)(5 * Intensity);
                if (blurSize % 2 == 0) blurSize++;
                Cv2.GaussianBlur(input, output, new Size(blurSize, blurSize), 0);
                break;

            case FilterType.Sharpen:
                var kernel = new Mat(3, 3, MatType.CV_32F, new float[] {
                    0, -1, 0,
                    -1, 5, -1,
                    0, -1, 0
                });
                Cv2.Filter2D(input, output, -1, kernel);
                kernel.Dispose();
                break;

            case FilterType.Vignette:
                output = ApplyVignette(input);
                break;
        }

        return output;
    }

    private Mat ApplyVignette(Mat input)
    {
        var output = input.Clone();
        var rows = input.Rows;
        var cols = input.Cols;

        var mask = new Mat(rows, cols, MatType.CV_32F, new Scalar(1));
        var center = new Point2f(cols / 2f, rows / 2f);

        for (int y = 0; y < rows; y++)
        {
            for (int x = 0; x < cols; x++)
            {
                double dist = Math.Sqrt(Math.Pow(x - center.X, 2) + Math.Pow(y - center.Y, 2));
                double maxDist = Math.Sqrt(Math.Pow(center.X, 2) + Math.Pow(center.Y, 2));
                double vignette = 1 - Math.Pow(dist / maxDist, 2) * Intensity;
                mask.Set(y, x, (float)vignette);
            }
        }

        var channels = new Mat[3];
        Cv2.Split(output, out channels);
        for (int i = 0; i < 3; i++)
        {
            channels[i].ConvertTo(channels[i], MatType.CV_32F);
            Cv2.Multiply(channels[i], mask, channels[i]);
            channels[i].ConvertTo(channels[i], MatType.CV_8UC1);
        }
        Cv2.Merge(channels, output);
        
        mask.Dispose();
        foreach (var c in channels) c.Dispose();
        
        return output;
    }
}