namespace CameraPro.Core.Interfaces;

public interface ICameraControls
{
    double Exposure { get; set; }
    double Focus { get; set; }
    int WhiteBalance { get; set; }
    double Brightness { get; set; }
    double Contrast { get; set; }
    double Saturation { get; set; }
    double Zoom { get; set; }

    bool IsAutoExposure { get; set; }
    bool IsAutoFocus { get; set; }
    bool IsAutoWhiteBalance { get; set; }

    void Reset();
    void ApplyToCamera(string cameraId);
}