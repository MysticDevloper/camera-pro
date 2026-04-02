using System.Windows.Media.Imaging;
using CameraPro.Core.Enums;
using CameraPro.Core.Models;

namespace CameraPro.Core.Interfaces;

public interface ICameraManager
{
    List<CameraDevice> GetCameras();
    void StartPreview(string cameraId);
    void StopPreview();
    BitmapSource? GetCurrentFrame();
    void SetFormat(CaptureSettings settings);
    CameraStatus Status { get; }
    
    event EventHandler<CameraStatus>? StatusChanged;
    event EventHandler<BitmapSource>? FrameAvailable;
    event EventHandler<string>? ErrorOccurred;
    event EventHandler? CameraDisconnected;
    event EventHandler<CameraDevice>? CameraAdded;
    event EventHandler<string>? CameraRemoved;
}
