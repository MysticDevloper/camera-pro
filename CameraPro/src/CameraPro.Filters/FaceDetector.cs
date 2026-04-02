using OpenCvSharp;
using System.IO;

namespace CameraPro.Filters;

public class FaceDetector
{
    private CascadeClassifier? _faceCascade;
    private CascadeClassifier? _eyeCascade;
    private bool _isInitialized;
    private readonly object _lock = new();

    public bool IsEnabled { get; set; } = true;
    public bool DrawRectangles { get; set; } = true;
    public Scalar RectangleColor { get; set; } = new Scalar(0, 255, 0);
    public int RectangleThickness { get; set; } = 2;

    public FaceDetector()
    {
        Initialize();
    }

    private void Initialize()
    {
        try
        {
            var cascadePath = FindCascadeFile("haarcascade_frontalface_default.xml");
            if (!string.IsNullOrEmpty(cascadePath))
            {
                _faceCascade = new CascadeClassifier(cascadePath);
            }

            var eyeCascadePath = FindCascadeFile("haarcascade_eye.xml");
            if (!string.IsNullOrEmpty(eyeCascadePath))
            {
                _eyeCascade = new CascadeClassifier(eyeCascadePath);
            }

            _isInitialized = _faceCascade != null && !_faceCascade.Empty();
        }
        catch (Exception ex)
        {
            Serilog.Log.Warning(ex, "Face detection initialization failed");
            _isInitialized = false;
        }
    }

    private string? FindCascadeFile(string fileName)
    {
        var searchPaths = new[]
        {
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "cascades", fileName),
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "data", fileName),
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "CameraPro", "cascades", fileName),
            $"/usr/share/opencv4/haarcascades/{fileName}",
            $"/usr/local/share/opencv4/haarcascades/{fileName}"
        };

        foreach (var path in searchPaths)
        {
            if (File.Exists(path))
                return path;
        }

        return null;
    }

    public bool IsReady => _isInitialized;

    public List<Rect> DetectFaces(Mat image)
    {
        var faces = new List<Rect>();

        lock (_lock)
        {
            if (!_isInitialized || image.Empty() || _faceCascade == null)
                return faces;

            try
            {
                var gray = new Mat();
                if (image.Channels() == 3)
                {
                    Cv2.CvtColor(image, gray, ColorConversionCodes.BGR2GRAY);
                }
                else
                {
                    image.CopyTo(gray);
                }

                Cv2.EqualizeHist(gray, gray);

                var detectedFaces = _faceCascade.DetectMultiScale(
                    gray,
                    scaleFactor: 1.1,
                    minNeighbors: 5,
                    flags: HaarDetectionTypes.ScaleImage,
                    minSize: new OpenCvSharp.Size(30, 30)
                );

                faces.AddRange(detectedFaces);

                gray.Dispose();
            }
            catch (Exception ex)
            {
                Serilog.Log.Warning(ex, "Face detection failed");
            }
        }

        return faces;
    }

    public List<Rect> DetectEyes(Mat image, Rect face)
    {
        var eyes = new List<Rect>();

        lock (_lock)
        {
            if (!_isInitialized || image.Empty() || _eyeCascade == null)
                return eyes;

            try
            {
                var faceRegion = new Mat(image, face);
                var gray = new Mat();
                
                if (faceRegion.Channels() == 3)
                {
                    Cv2.CvtColor(faceRegion, gray, ColorConversionCodes.BGR2GRAY);
                }
                else
                {
                    faceRegion.CopyTo(gray);
                }

                Cv2.EqualizeHist(gray, gray);

                var detectedEyes = _eyeCascade.DetectMultiScale(
                    gray,
                    scaleFactor: 1.1,
                    minNeighbors: 5,
                    flags: HaarDetectionTypes.ScaleImage,
                    minSize: new OpenCvSharp.Size(15, 15)
                );

                foreach (var eye in detectedEyes)
                {
                    eyes.Add(new Rect(face.X + eye.X, face.Y + eye.Y, eye.Width, eye.Height));
                }

                gray.Dispose();
                faceRegion.Dispose();
            }
            catch (Exception ex)
            {
                Serilog.Log.Warning(ex, "Eye detection failed");
            }
        }

        return eyes;
    }

    public Mat DrawFaceRectangles(Mat input)
    {
        if (!IsEnabled || !IsReady || input.Empty())
            return input;

        var result = input.Clone();
        var faces = DetectFaces(input);

        foreach (var face in faces)
        {
            if (DrawRectangles)
            {
                Cv2.Rectangle(result, face, RectangleColor, RectangleThickness);
            }

            var eyes = DetectEyes(input, face);
            foreach (var eye in eyes.Take(2))
            {
                Cv2.Rectangle(result, eye, new Scalar(255, 255, 0), 1);
            }
        }

        return result;
    }

    public (int x, int y)? GetLargestFaceCenter(Mat image)
    {
        var faces = DetectFaces(image);
        if (faces.Count == 0)
            return null;

        var largest = faces.OrderByDescending(f => f.Width * f.Height).First();
        return (largest.X + largest.Width / 2, largest.Y + largest.Height / 2);
    }

    public void Dispose()
    {
        lock (_lock)
        {
            _faceCascade?.Dispose();
            _eyeCascade?.Dispose();
            _faceCascade = null;
            _eyeCascade = null;
            _isInitialized = false;
        }
    }
}