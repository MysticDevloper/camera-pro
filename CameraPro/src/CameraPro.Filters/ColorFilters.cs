using OpenCvSharp;
using CameraPro.Core.Enums;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows;

namespace CameraPro.Filters;

public class ColorFilters : IFilter
{
    public string Name => Type.ToString();
    public bool IsEnabled { get; set; } = true;
    public Dictionary<string, double> Parameters { get; set; } = new();
    public FilterType Type { get; set; }
    public double Intensity { get; set; } = 1.0;

    public Mat Apply(Mat input)
    {
        if (!IsEnabled || input.Empty()) return input;

        Mat output = input.Clone();

        switch (Type)
        {
            case FilterType.Grayscale:
                Cv2.CvtColor(input, output, ColorConversionCodes.BGR2GRAY);
                Cv2.CvtColor(output, output, ColorConversionCodes.GRAY2BGR);
                break;

            case FilterType.Sepia:
                var sepia = new Mat(input.Size(), MatType.CV_8UC3);
                var kernel = new Mat(3, 3, MatType.CV_32F, new float[] {
                    0.272f, 0.534f, 0.131f,
                    0.349f, 0.686f, 0.168f,
                    0.393f, 0.769f, 0.189f
                });
                Cv2.Transform(input, sepia, kernel);
                sepia.ConvertTo(output, MatType.CV_8UC3);
                sepia.Dispose();
                break;

            case FilterType.Warm:
                var warmInput = input.Clone();
                var warmOutput = new Mat();
                Cv2.CvtColor(warmInput, warmOutput, ColorConversionCodes.BGR2HSV);
                Cv2.CvtColor(warmOutput, output, ColorConversionCodes.HSV2BGR);
                warmInput.Dispose();
                warmOutput.Dispose();
                break;

            case FilterType.Cool:
                Cv2.Split(input, out var channels);
                Cv2.AddWeighted(channels[0], 1.2, channels[0], 0, 20, channels[0]);
                Cv2.Merge(channels, output);
                break;

            case FilterType.Negative:
                Cv2.BitwiseNot(input, output);
                break;
        }

        return output;
    }
}

public enum GrayscaleMethod
{
    Average,
    Luminosity,
    Desaturation
}