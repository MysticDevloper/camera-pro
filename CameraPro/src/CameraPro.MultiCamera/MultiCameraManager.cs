using CameraPro.Core.Interfaces;
using CameraPro.Core.Models;
using System.Windows.Media.Imaging;
using Serilog;

namespace CameraPro.MultiCamera;

public class MultiCameraManager : IDisposable
{
    private readonly Dictionary<string, ICameraManager> _cameras = new();
    private readonly Dictionary<string, BitmapSource?> _latestFrames = new();
    
    public event EventHandler<Dictionary<string, BitmapSource>>? FramesUpdated;
    
    public void AddCamera(string cameraId, ICameraManager camera)
    {
        if (_cameras.ContainsKey(cameraId)) return;
        
        _cameras[cameraId] = camera;
        _latestFrames[cameraId] = null;
        
        camera.FrameAvailable += (s, frame) =>
        {
            _latestFrames[cameraId] = frame;
            FramesUpdated?.Invoke(this, new Dictionary<string, BitmapSource>(_latestFrames));
        };
        
        camera.StartPreview(cameraId);
        Log.Information("Added camera to multi-camera: {CameraId}", cameraId);
    }
    
    public void RemoveCamera(string cameraId)
    {
        if (!_cameras.ContainsKey(cameraId)) return;
        
        _cameras[cameraId].StopPreview();
        _cameras.Remove(cameraId);
        _latestFrames.Remove(cameraId);
    }
    
    public Dictionary<string, BitmapSource?> GetAllFrames() => new(_latestFrames);
    
    public void Dispose()
    {
        foreach (var camera in _cameras.Values)
        {
            camera.StopPreview();
        }
        _cameras.Clear();
    }
}
