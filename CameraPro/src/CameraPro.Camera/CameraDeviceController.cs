using CameraPro.Core.Models;
using Serilog;
using Windows.Media.Capture;
using Windows.Media.Devices;

namespace CameraPro.Camera;

public class CameraDeviceController : IDisposable
{
    private MediaCapture? _mediaCapture;
    private VideoDeviceController? _videoDeviceController;
    private string? _cameraId;
    private ControlPreset? _lastPreset;

    public event EventHandler<ControlCapability>? CapabilityDetected;

    public bool IsConnected => _videoDeviceController != null;

    public void Connect(MediaCapture mediaCapture, string cameraId)
    {
        _mediaCapture = mediaCapture;
        _cameraId = cameraId;
        _videoDeviceController = mediaCapture.VideoDeviceController;
        Log.Information("CameraDeviceController connected to camera: {CameraId}", cameraId);
    }

    public void Disconnect()
    {
        _mediaCapture = null;
        _videoDeviceController = null;
        _cameraId = null;
    }

    public ControlCapability GetExposureCapability()
    {
        if (_videoDeviceController?.Exposure == null)
        {
            return new ControlCapability { ControlName = "Exposure", IsSupported = false };
        }

        try
        {
            var exposureControl = _videoDeviceController.Exposure;
            return new ControlCapability
            {
                ControlName = "Exposure",
                IsSupported = true,
                MinValue = exposureControl.Min,
                MaxValue = exposureControl.Max,
                DefaultValue = exposureControl.Value,
                Step = exposureControl.Step,
                IsAutoSupported = exposureControl.AutoModeSupported,
                IsManualSupported = exposureControl.ManualModeSupported
            };
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Failed to get exposure capability");
            return new ControlCapability { ControlName = "Exposure", IsSupported = false };
        }
    }

    public ControlCapability GetFocusCapability()
    {
        if (_videoDeviceController?.Focus == null)
        {
            return new ControlCapability { ControlName = "Focus", IsSupported = false };
        }

        try
        {
            var focusControl = _videoDeviceController.Focus;
            return new ControlCapability
            {
                ControlName = "Focus",
                IsSupported = true,
                MinValue = focusControl.Min,
                MaxValue = focusControl.Max,
                DefaultValue = focusControl.Value,
                Step = focusControl.Step,
                IsAutoSupported = focusControl.AutoModeSupported,
                IsManualSupported = focusControl.ManualModeSupported
            };
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Failed to get focus capability");
            return new ControlCapability { ControlName = "Focus", IsSupported = false };
        }
    }

    public ControlCapability GetWhiteBalanceCapability()
    {
        if (_videoDeviceController?.WhiteBalance == null)
        {
            return new ControlCapability { ControlName = "WhiteBalance", IsSupported = false };
        }

        try
        {
            var wbControl = _videoDeviceController.WhiteBalance;
            return new ControlCapability
            {
                ControlName = "WhiteBalance",
                IsSupported = true,
                MinValue = wbControl.Min,
                MaxValue = wbControl.Max,
                DefaultValue = wbControl.Value,
                Step = wbControl.Step,
                IsAutoSupported = wbControl.AutoModeSupported,
                IsManualSupported = wbControl.ManualModeSupported
            };
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Failed to get white balance capability");
            return new ControlCapability { ControlName = "WhiteBalance", IsSupported = false };
        }
    }

    public List<ControlCapability> DetectAllCapabilities()
    {
        var capabilities = new List<ControlCapability>
        {
            GetExposureCapability(),
            GetFocusCapability(),
            GetWhiteBalanceCapability(),
            GetZoomCapability()
        };

        foreach (var cap in capabilities.Where(c => c.IsSupported))
        {
            CapabilityDetected?.Invoke(this, cap);
        }

        return capabilities;
    }

    private ControlCapability GetZoomCapability()
    {
        if (_videoDeviceController?.Zoom == null)
        {
            return new ControlCapability { ControlName = "Zoom", IsSupported = false };
        }

        try
        {
            var zoomControl = _videoDeviceController.Zoom;
            return new ControlCapability
            {
                ControlName = "Zoom",
                IsSupported = true,
                MinValue = zoomControl.Min,
                MaxValue = zoomControl.Max,
                DefaultValue = zoomControl.Value,
                Step = zoomControl.Step
            };
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Failed to get zoom capability");
            return new ControlCapability { ControlName = "Zoom", IsSupported = false };
        }
    }

    public void SetExposure(double value, bool auto)
    {
        if (_videoDeviceController?.Exposure == null)
        {
            Log.Warning("Exposure control not supported");
            return;
        }

        try
        {
            var exposureControl = _videoDeviceController.Exposure;
            if (auto)
            {
                exposureControl.TrySetAuto(true);
                Log.Information("Exposure set to Auto");
            }
            else
            {
                exposureControl.TrySetAuto(false);
                var clampedValue = Math.Clamp(value, exposureControl.Min, exposureControl.Max);
                exposureControl.TrySetValue(clampedValue);
                Log.Information("Exposure set to {Value}", clampedValue);
            }
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Failed to set exposure");
        }
    }

    public void SetFocus(double value, bool auto)
    {
        if (_videoDeviceController?.Focus == null)
        {
            Log.Warning("Focus control not supported");
            return;
        }

        try
        {
            var focusControl = _videoDeviceController.Focus;
            if (auto)
            {
                focusControl.TrySetAuto(true);
                Log.Information("Focus set to Auto");
            }
            else
            {
                focusControl.TrySetAuto(false);
                var clampedValue = Math.Clamp(value, focusControl.Min, focusControl.Max);
                focusControl.TrySetValue(clampedValue);
                Log.Information("Focus set to {Value}", clampedValue);
            }
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Failed to set focus");
        }
    }

    public void SetWhiteBalance(int kelvin, bool auto)
    {
        if (_videoDeviceController?.WhiteBalance == null)
        {
            Log.Warning("White balance control not supported");
            return;
        }

        try
        {
            var wbControl = _videoDeviceController.WhiteBalance;
            if (auto)
            {
                wbControl.TrySetAuto(true);
                Log.Information("White balance set to Auto");
            }
            else
            {
                wbControl.TrySetAuto(false);
                var clampedValue = Math.Clamp(kelvin, (int)wbControl.Min, (int)wbControl.Max);
                wbControl.TrySetValue(clampedValue);
                Log.Information("White balance set to {Kelvin}K", clampedValue);
            }
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Failed to set white balance");
        }
    }

    public void SetZoom(double value)
    {
        if (_videoDeviceController?.Zoom == null)
        {
            Log.Warning("Zoom control not supported");
            return;
        }

        try
        {
            var zoomControl = _videoDeviceController.Zoom;
            var clampedValue = Math.Clamp(value, zoomControl.Min, zoomControl.Max);
            zoomControl.TrySetValue(clampedValue);
            Log.Information("Zoom set to {Zoom}x", clampedValue);
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Failed to set zoom");
        }
    }

    public bool IsAutoExposureEnabled => _videoDeviceController?.Exposure?.AutoModeSupported ?? true;

    public void SetAutoExposure(bool auto)
    {
        if (_videoDeviceController?.Exposure == null)
        {
            Log.Warning("Exposure control not supported");
            return;
        }

        try
        {
            var exposureControl = _videoDeviceController.Exposure;
            if (auto)
            {
                exposureControl.TrySetAuto(true);
                Log.Information("Auto exposure enabled");
            }
            else
            {
                exposureControl.TrySetAuto(false);
                var minValue = exposureControl.Min;
                exposureControl.TrySetValue(minValue);
                Log.Information("Auto exposure disabled, set to min: {Value}", minValue);
            }
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Failed to set auto exposure");
        }
    }

    public bool IsAutoFocusEnabled => _videoDeviceController?.Focus?.AutoModeSupported ?? true;

    public void SetAutoFocus(bool auto)
    {
        if (_videoDeviceController?.Focus == null)
        {
            Log.Warning("Focus control not supported");
            return;
        }

        try
        {
            var focusControl = _videoDeviceController.Focus;
            if (auto)
            {
                focusControl.TrySetAuto(true);
                Log.Information("Auto focus enabled");
            }
            else
            {
                focusControl.TrySetAuto(false);
                Log.Information("Auto focus disabled");
            }
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Failed to set auto focus");
        }
    }

    public void SetWhiteBalance(int kelvin)
    {
        if (_videoDeviceController?.WhiteBalance == null)
        {
            Log.Warning("White balance control not supported");
            return;
        }

        try
        {
            var wbControl = _videoDeviceController.WhiteBalance;
            wbControl.TrySetAuto(false);

            var clampedValue = Math.Clamp(kelvin, (int)wbControl.Min, (int)wbControl.Max);
            wbControl.TrySetValue(clampedValue);
            Log.Information("White balance set to {Kelvin}K", clampedValue);
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Failed to set white balance");
        }
    }

    public void SetBrightness(double value)
    {
        if (_videoDeviceController == null) return;

        try
        {
            if (_videoDeviceController.Brightness.TryGetValue(out var current))
            {
                var range = 1.0;
                var newValue = Math.Clamp(value, -range, range);
                _videoDeviceController.Brightness.TrySetValue(newValue);
                Log.Information("Brightness set to {Value}", newValue);
            }
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Failed to set brightness");
        }
    }

    public void SetContrast(double value)
    {
        if (_videoDeviceController == null) return;

        try
        {
            if (_videoDeviceController.Contrast.TryGetValue(out var current))
            {
                var range = 1.0;
                var newValue = Math.Clamp(value, -range, range);
                _videoDeviceController.Contrast.TrySetValue(newValue);
                Log.Information("Contrast set to {Value}", newValue);
            }
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Failed to set contrast");
        }
    }

    public void SetSaturation(double value)
    {
        if (_videoDeviceController == null) return;

        try
        {
            if (_videoDeviceController.Saturation.TryGetValue(out var current))
            {
                var range = 1.0;
                var newValue = Math.Clamp(value, -range, range);
                _videoDeviceController.Saturation.TrySetValue(newValue);
                Log.Information("Saturation set to {Value}", newValue);
            }
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Failed to set saturation");
        }
    }

    public void ApplyPreset(ControlPreset preset)
    {
        _lastPreset = preset;
        SetExposure(preset.Exposure, preset.IsAutoExposure);
        SetFocus(preset.Focus, preset.IsAutoFocus);
        
        if (preset.IsAutoWhiteBalance)
        {
            try
            {
                _videoDeviceController?.WhiteBalance?.TrySetAuto(true);
            }
            catch { }
        }
        else
        {
            SetWhiteBalance(preset.WhiteBalance);
        }

        SetBrightness(preset.Brightness);
        SetContrast(preset.Contrast);
        SetSaturation(preset.Saturation);
    }

    public void ReapplyControls()
    {
        if (_lastPreset != null)
        {
            ApplyPreset(_lastPreset);
        }
    }

    public void Dispose()
    {
        Disconnect();
    }
}