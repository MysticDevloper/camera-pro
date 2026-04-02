using System.Collections.ObjectModel;
using CameraPro.Core.Enums;
using CameraPro.Core.Models;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Serilog;

namespace CameraPro.Camera;

public partial class CameraSelectorViewModel : ObservableObject
{
    private readonly CameraManager _cameraManager;

    [ObservableProperty]
    private ObservableCollection<CameraDevice> _cameras = new();

    [ObservableProperty]
    private CameraDevice? _selectedCamera;

    [ObservableProperty]
    private CameraStatus _cameraStatus = CameraStatus.Disconnected;

    [ObservableProperty]
    private bool _isPreviewActive;

    public CameraSelectorViewModel(CameraManager cameraManager)
    {
        _cameraManager = cameraManager;
        _cameraManager.StatusChanged += OnCameraStatusChanged;
    }

    private void OnCameraStatusChanged(object? sender, CameraStatus status)
    {
        CameraStatus = status;
        IsPreviewActive = status == CameraStatus.Connected;
    }

    [RelayCommand]
    private void RefreshCameras()
    {
        try
        {
            var cameras = _cameraManager.GetCameras();
            Cameras.Clear();
            foreach (var camera in cameras)
            {
                Cameras.Add(camera);
            }

            if (Cameras.Any() && SelectedCamera == null)
            {
                SelectedCamera = Cameras.FirstOrDefault(c => c.IsDefault) ?? Cameras.First();
            }

            Log.Information("Refreshed camera list: {Count} cameras found", Cameras.Count);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to refresh cameras");
        }
    }

    [RelayCommand]
    private void StartPreview()
    {
        if (SelectedCamera != null)
        {
            _cameraManager.StartPreview(SelectedCamera.Id);
        }
    }

    [RelayCommand]
    private void StopPreview()
    {
        _cameraManager.StopPreview();
    }

    partial void OnSelectedCameraChanged(CameraDevice? value)
    {
        if (value != null && IsPreviewActive)
        {
            _cameraManager.StartPreview(value.Id);
        }
    }
}
