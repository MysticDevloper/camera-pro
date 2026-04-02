using OpenCvSharp;

namespace CameraPro.Filters;

public interface IFilter
{
    string Name { get; }
    bool IsEnabled { get; set; }
    Dictionary<string, double> Parameters { get; set; }
    Mat Apply(Mat input);
}