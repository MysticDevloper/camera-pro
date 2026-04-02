using CameraPro.Camera;
using CameraPro.Controls.Color;
using CameraPro.Controls.Exposure;
using CameraPro.Controls.Focus;
using CameraPro.Controls.Presets;
using CameraPro.Controls.WhiteBalance;
using CameraPro.Controls.Zoom;
using CameraPro.Core.Interfaces;
using CameraPro.Core.Models;
using CommunityToolkit.Mvvm.ComponentModel;

namespace CameraPro.Controls;

public partial class CameraControls : ObservableObject, ICameraControls
{
    private string? _currentCameraId;
    private readonly Dictionary<string, List<ControlCapability>> _capabilitiesCache = new();
    private CameraManager? _cameraManager;

    [ObservableProperty]
    private ExposureControl _exposure = new();

    [ObservableProperty]
    private FocusControl _focus = new();

    [ObservableProperty]
    private WhiteBalanceControl _whiteBalance = new();

    [ObservableProperty]
    private ColorControls _color = new();

    [ObservableProperty]
    private ZoomControl _zoom = new();

    [ObservableProperty]
    private PresetManager _presetManager = new();

    [ObservableProperty]
    private ControlPreset? _currentPreset;

    public double Exposure
    {
        get => ExposureControl.Exposure;
        set => ExposureControl.Exposure = value;
    }

    public double Focus
    {
        get => FocusControl.Focus;
        set => FocusControl.Focus = value;
    }

    public int WhiteBalance
    {
        get => WhiteBalanceControl.WhiteBalance;
        set => WhiteBalanceControl.WhiteBalance = value;
    }

    public double Brightness
    {
        get => Color.Brightness;
        set
        {
            Color.Brightness = value;
            if (_cameraManager?.DeviceController.IsConnected == true)
            {
                _cameraManager.DeviceController.SetBrightness(value / 100.0);
            }
        }
    }

    public double Contrast
    {
        get => Color.Contrast;
        set
        {
            Color.Contrast = value;
            if (_cameraManager?.DeviceController.IsConnected == true)
            {
                _cameraManager.DeviceController.SetContrast(value / 100.0);
            }
        }
    }

    public double Saturation
    {
        get => Color.Saturation;
        set
        {
            Color.Saturation = value;
            if (_cameraManager?.DeviceController.IsConnected == true)
            {
                _cameraManager.DeviceController.SetSaturation(value / 100.0);
            }
        }
    }

    public double Zoom
    {
        get => ZoomControl.Zoom;
        set => ZoomControl.Zoom = value;
    }

    public bool IsAutoExposure
    {
        get => ExposureControl.IsAuto;
        set
        {
            ExposureControl.IsAuto = value;
            if (_cameraManager?.DeviceController.IsConnected == true)
            {
                _cameraManager.DeviceController.SetAutoExposure(value);
            }
        }
    }

    public bool IsAutoFocus
    {
        get => FocusControl.Mode == Core.Enums.FocusMode.Auto || FocusControl.Mode == Core.Enums.FocusMode.Continuous;
        set
        {
            if (value)
                FocusControl.SetModeAuto();
            if (_cameraManager?.DeviceController.IsConnected == true)
            {
                _cameraManager.DeviceController.SetAutoFocus(value);
            }
        }
    }

    public bool IsAutoWhiteBalance
    {
        get => WhiteBalanceControl.IsAuto;
        set
        {
            WhiteBalanceControl.IsAuto = value;
            if (!value && _cameraManager?.DeviceController.IsConnected == true)
            {
                _cameraManager.DeviceController.SetWhiteBalance(WhiteBalanceControl.WhiteBalance);
            }
        }
    }

    public CameraControls()
    {
        Reset();
    }

    public void BindToCamera(CameraManager cameraManager)
    {
        _cameraManager = cameraManager;
        if (!string.IsNullOrEmpty(_currentCameraId))
        {
            var caps = cameraManager.DeviceController.DetectAllCapabilities();
            _capabilitiesCache[_currentCameraId] = caps;
        }
    }

    public void BindToCamera(string cameraId, CameraDeviceController deviceController)
    {
        _currentCameraId = cameraId;
        var caps = deviceController.DetectAllCapabilities();
        _capabilitiesCache[cameraId] = caps;
    }

    public void InitializeForCamera(string cameraId)
    {
        _currentCameraId = cameraId;
        DetectCapabilitiesFromDevice();
    }

    private void DetectCapabilitiesFromDevice()
    {
        if (_cameraManager?.DeviceController.IsConnected == true)
        {
            var caps = _cameraManager.DeviceController.DetectAllCapabilities();
            if (_currentCameraId != null)
            {
                _capabilitiesCache[_currentCameraId] = caps;
            }
        }
    }

    public List<ControlCapability> GetCapabilities(string cameraId)
    {
        return _capabilitiesCache.TryGetValue(cameraId, out var caps) ? caps : new List<ControlCapability>();
    }

    public void Reset()
    {
        Exposure.Reset();
        Focus.Reset();
        WhiteBalance.Reset();
        Color.Reset();
        Zoom.Reset();
        CurrentPreset = null;
    }

    public void ApplyToCamera(string cameraId)
    {
        if (_currentCameraId != cameraId)
        {
            InitializeForCamera(cameraId);
        }
    }

    public void LoadPreset(ControlPreset preset)
    {
        CurrentPreset = preset;

        ExposureControl.Exposure = preset.Exposure;
        ExposureControl.IsAuto = preset.IsAutoExposure;

        FocusControl.Focus = preset.Focus;
        FocusControl.IsAuto = preset.IsAutoFocus;

        WhiteBalanceControl.WhiteBalance = preset.WhiteBalance;
        WhiteBalanceControl.IsAuto = preset.IsAutoWhiteBalance;

        Color.Brightness = preset.Brightness;
        Color.Contrast = preset.Contrast;
        Color.Saturation = preset.Saturation;

        Zoom.Zoom = preset.Zoom;

        ApplyAllToCamera();
    }

    public void ApplyAllToCamera()
    {
        if (_cameraManager?.DeviceController.IsConnected == true)
        {
            _cameraManager.DeviceController.SetExposure(ExposureControl.Exposure, ExposureControl.IsAuto);
            _cameraManager.DeviceController.SetFocus(FocusControl.Focus, FocusControl.Mode != Core.Enums.FocusMode.Manual);
            _cameraManager.DeviceController.SetWhiteBalance(WhiteBalanceControl.WhiteBalance, WhiteBalanceControl.IsAuto);
            _cameraManager.DeviceController.SetZoom(ZoomControl.Zoom);
        }
    }

    public void ApplyExposureToCamera()
    {
        if (_cameraManager?.DeviceController.IsConnected == true)
        {
            _cameraManager.DeviceController.SetExposure(ExposureControl.Exposure, ExposureControl.IsAuto);
        }
    }

    public void ApplyFocusToCamera()
    {
        if (_cameraManager?.DeviceController.IsConnected == true)
        {
            _cameraManager.DeviceController.SetFocus(FocusControl.Focus, FocusControl.Mode != Core.Enums.FocusMode.Manual);
        }
    }

    public void ApplyWhiteBalanceToCamera()
    {
        if (_cameraManager?.DeviceController.IsConnected == true)
        {
            _cameraManager.DeviceController.SetWhiteBalance(WhiteBalanceControl.WhiteBalance, WhiteBalanceControl.IsAuto);
        }
    }

    public void ApplyZoomToCamera()
    {
        if (_cameraManager?.DeviceController.IsConnected == true)
        {
            _cameraManager.DeviceController.SetZoom(ZoomControl.Zoom);
        }
    }

    public void SaveCurrentAsPreset(string name, string? cameraId = null)
    {
        PresetManager.SaveCurrentAsPreset(
            name,
            cameraId ?? _currentCameraId,
            Exposure,
            Focus,
            WhiteBalance,
            Brightness,
            Contrast,
            Saturation,
            IsAutoExposure,
            IsAutoFocus,
            IsAutoWhiteBalance
        );
    }

    public void ApplyPreset(string presetName)
    {
        var preset = PresetManager.GetPreset(presetName);
        if (preset != null)
        {
            LoadPreset(preset);
        }
    }
}