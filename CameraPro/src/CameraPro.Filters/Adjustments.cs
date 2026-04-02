using OpenCvSharp;

namespace CameraPro.Filters;

public class Adjustments : IFilter
{
    public string Name => "Adjustments";
    public bool IsEnabled { get; set; } = true;
    public Dictionary<string, double> Parameters { get; set; } = new()
    {
        ["Brightness"] = 0,
        ["Contrast"] = 0,
        ["Saturation"] = 0,
        ["Gamma"] = 1.0
    };

    public Mat Apply(Mat input)
    {
        if (!IsEnabled || input.Empty()) return input;

        double brightness = Parameters.GetValueOrDefault("Brightness", 0);
        double contrast = Parameters.GetValueOrDefault("Contrast", 0);
        double saturation = Parameters.GetValueOrDefault("Saturation", 0);
        double gamma = Parameters.GetValueOrDefault("Gamma", 1.0);

        Mat output = input.Clone();

        double alpha = 1 + contrast / 100.0;
        double beta = brightness;
        input.ConvertTo(output, -1, alpha, beta);

        if (gamma != 1.0)
        {
            var lut = new Mat(256, 1, MatType.CV_8UC1);
            for (int i = 0; i < 256; i++)
            {
                lut.Set(i, 0, (byte)Math.Pow(i / 255.0, 1 / gamma) * 255);
            }
            Cv2.LUT(output, lut, output);
            lut.Dispose();
        }

        if (saturation != 0)
        {
            var hsv = new Mat();
            Cv2.CvtColor(output, hsv, ColorConversionCodes.BGR2HSV);
            var channels = new Mat[3];
            Cv2.Split(hsv, out channels);
            double satFactor = 1 + saturation / 100.0;
            channels[1].ConvertTo(channels[1], MatType.CV_8UC1, satFactor);
            Cv2.Merge(channels, hsv);
            Cv2.CvtColor(hsv, output, ColorConversionCodes.HSV2BGR);
            hsv.Dispose();
            foreach (var c in channels) c.Dispose();
        }

        return output;
    }
}