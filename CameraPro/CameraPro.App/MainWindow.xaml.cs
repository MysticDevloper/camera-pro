using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using CameraPro.Core.Interfaces;
using CameraPro.Core.Models;
using CameraPro.Core.Enums;
using CameraPro.Camera;
using CameraPro.Capture;
using CameraPro.Controls;
using CameraPro.Filters;
using CameraPro.Storage;
using Serilog;

namespace CameraPro.App;

public partial class MainWindow : Window
{
    private readonly ICameraManager _cameraManager;
    private readonly IVideoRecorder _videoRecorder;
    private readonly CameraRecorderBridge _recorderBridge;
    private readonly IPhotoCapture _photoCapture;
    private readonly CameraControls _cameraControls;
    private readonly IFileManager _fileManager;
    private readonly IMediaLibrary _mediaLibrary;
    private readonly FilterService _filterPipeline;
    
    private DispatcherTimer? _previewTimer;
    private DispatcherTimer? _recordingTimer;
    private DispatcherTimer? _countdownTimer;
    private DateTime _recordingStartTime;
    private TimeSpan _recordingDuration;
    private bool _isRecording;
    private bool _isPaused;
    private bool _isFullscreen;
    private double _zoomLevel = 1.0;
    private int _countdownValue;
    private string? _lastPhotoPath;
    private bool _filtersPanelVisible = true;
    private bool _controlsPanelVisible = true;
    private bool _showGrid;

    public MainWindow()
    {
        InitializeComponent();
        
        _cameraManager = App.Services.GetRequiredService<ICameraManager>();
        _videoRecorder = App.Services.GetRequiredService<IVideoRecorder>();
        _recorderBridge = App.Services.GetRequiredService<CameraRecorderBridge>();
        _photoCapture = App.Services.GetRequiredService<IPhotoCapture>();
        _cameraControls = App.Services.GetRequiredService<CameraControls>();
        _fileManager = App.Services.GetRequiredService<IFileManager>();
        _mediaLibrary = App.Services.GetRequiredService<IMediaLibrary>();
        _filterPipeline = App.Services.GetRequiredService<FilterService>();
        
        // Subscribe to camera events
        _cameraManager.StatusChanged += OnCameraStatusChanged;
        
        InitializeCameraSelector();
        InitializePreviewTimer();
        UpdateStorageStatus();
        
        Log.Information("MainWindow initialized");
    }

    private void OnCameraStatusChanged(object? sender, CameraStatus status)
    {
        Dispatcher.Invoke(() =>
        {
            CameraStatusText.Text = status.ToString();
        });
    }

    private void InitializeCameraSelector()
    {
        try
        {
            var cameras = _cameraManager.GetCameras();
            CameraSelector.Items.Clear();
            CameraSelector.Items.Add(new ComboBoxItem { Content = "Select Camera..." });
            
            foreach (var camera in cameras)
            {
                CameraSelector.Items.Add(new ComboBoxItem 
                { 
                    Content = camera.Name,
                    Tag = camera.Id
                });
            }
            
            if (cameras.Count > 0)
            {
                CameraSelector.SelectedIndex = 1;
                CameraStatusText.Text = $"{cameras.Count} camera(s) found";
            }
            else
            {
                CameraStatusText.Text = "No cameras found";
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to initialize camera selector");
        }
    }

    private void InitializePreviewTimer()
    {
        _previewTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(33)
        };
        _previewTimer.Tick += PreviewTimer_Tick;
    }

    private void PreviewTimer_Tick(object? sender, EventArgs e)
    {
        try
        {
            var frame = _cameraManager.GetCurrentFrame();
            if (frame != null)
            {
                BitmapSource displayFrame = frame;
                
                if (_filterPipeline.Pipeline.Filters.Any())
                {
                    displayFrame = _filterPipeline.ProcessAndConvertToBitmapSource(frame) ?? frame;
                }
                
                if (_zoomLevel != 1.0)
                {
                    PreviewImage.LayoutTransform = new ScaleTransform(_zoomLevel, _zoomLevel);
                }
                else
                {
                    PreviewImage.LayoutTransform = null;
                }
                
                PreviewImage.Source = displayFrame;
                PreviewStatusText.Text = "Preview Active";
            }
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Error updating preview frame");
        }
    }

    private void CameraSelector_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (CameraSelector.SelectedIndex > 0 && CameraSelector.SelectedItem is ComboBoxItem item && item.Tag is string cameraId)
        {
            try
            {
                _cameraManager.StartPreview(cameraId);
                _previewTimer?.Start();
                CameraStatusText.Text = $"Camera: {item.Content}";
                _cameraControls.InitializeForCamera(cameraId);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to start preview");
                CameraStatusText.Text = "Camera Error";
            }
        }
    }

    private void ResolutionSelector_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        UpdateResolutionStatus();
    }

    private void FpsSelector_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        UpdateResolutionStatus();
    }

    private void UpdateResolutionStatus()
    {
        if (ResolutionSelector.SelectedItem is ComboBoxItem resItem && FpsSelector.SelectedItem is ComboBoxItem fpsItem)
        {
            ResolutionStatusText.Text = $"{resItem.Content?.ToString()?.Split(' ')[1]} @ {fpsItem.Content}fps";
        }
    }

    private void PhotoButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var timerIndex = QuickTimerSelector.SelectedIndex;
            if (timerIndex > 0)
            {
                _countdownValue = timerIndex == 1 ? 3 : timerIndex == 2 ? 5 : 10;
                StartCountdown();
            }
            else
            {
                CapturePhoto();
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to capture photo");
            ShowToast("Failed to capture photo");
        }
    }

    private void StartCountdown()
    {
        TimerOverlay.Visibility = Visibility.Visible;
        TimerOverlay.Text = _countdownValue.ToString();
        
        _countdownTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        _countdownTimer.Tick += (s, args) =>
        {
            _countdownValue--;
            if (_countdownValue > 0)
            {
                TimerOverlay.Text = _countdownValue.ToString();
            }
            else
            {
                _countdownTimer?.Stop();
                TimerOverlay.Visibility = Visibility.Collapsed;
                CapturePhoto();
            }
        };
        _countdownTimer.Start();
    }

    private async void CapturePhoto()
    {
        try
        {
            var path = await _photoCapture.CapturePhotoAsync();
            ShowLastPhoto(path);
            ShowToast($"Photo saved: {System.IO.Path.GetFileName(path)}");
            UpdateStorageStatus();
            CameraStatusText.Text = "Photo captured";
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to capture photo");
            ShowToast("Failed to capture photo");
        }
    }

    private void ShowLastPhoto(string path)
    {
        try
        {
            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.UriSource = new Uri(path);
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.EndInit();
            bitmap.Freeze();
            
            LastPhotoImage.Source = bitmap;
            LastPhotoThumbnail.Visibility = Visibility.Visible;
            _lastPhotoPath = path;
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Failed to load last photo thumbnail");
        }
    }

    private void BurstButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var burstIndex = BurstModeSelector.SelectedIndex;
            if (burstIndex == 0) return;
            
            var count = burstIndex == 1 ? 3 : burstIndex == 2 ? 5 : burstIndex == 3 ? 10 : 5;
            var paths = _photoCapture.CaptureBurst(count);
            
            if (paths.Count > 0)
            {
                ShowLastPhoto(paths[^1]);
                ShowToast($"Burst: {count} photos captured");
            }
            UpdateStorageStatus();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to capture burst");
        }
    }

    private void RecordButton_Click(object sender, RoutedEventArgs e)
    {
        if (!_isRecording)
        {
            StartRecording();
        }
        else
        {
            StopRecording();
        }
    }

    private void StartRecording()
    {
        try
        {
            var settings = new CaptureSettings
            {
                Resolution = new Resolution { Width = 1920, Height = 1080 },
                FrameRate = 30
            };
            
            var outputPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.MyVideos),
                "CameraPro",
                DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss") + ".mp4");
            
            Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);
            
            _recorderBridge.StartRecordingAsync(outputPath, settings).Wait();
            
            _isRecording = true;
            _recordingStartTime = DateTime.Now;
            _recordingDuration = TimeSpan.Zero;
            
            RecordDot.Fill = new SolidColorBrush(Colors.Red);
            RecordingIndicator.Visibility = Visibility.Visible;
            PauseButton.IsEnabled = true;
            CameraStatusText.Text = "Recording";
            
            _recordingTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            _recordingTimer.Tick += RecordingTimer_Tick;
            _recordingTimer.Start();
            
            ShowToast("Recording started");
            Log.Information("Recording started");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to start recording");
            _isRecording = false;
        }
    }

    private void RecordingTimer_Tick(object? sender, EventArgs e)
    {
        if (!_isPaused)
        {
            _recordingDuration = DateTime.Now - _recordingStartTime;
            RecordingTimeText.Text = _recordingDuration.ToString(@"hh\:mm\:ss");
            
            var estimatedSize = _recordingDuration.TotalSeconds * 10 * 1024 * 1024 / 8;
            FileSizeText.Text = $"{estimatedSize / (1024 * 1024):F1} MB";
        }
    }

    private void StopRecording()
    {
        try
        {
            var session = _recorderBridge.StopRecording();
            
            _isRecording = false;
            _isPaused = false;
            _recordingTimer?.Stop();
            
            RecordDot.Fill = new SolidColorBrush(Color.FromRgb(136, 136, 136));
            RecordingIndicator.Visibility = Visibility.Collapsed;
            PauseButton.IsEnabled = false;
            PauseButton.Content = "Pause";
            CameraStatusText.Text = "Ready";
            
            ShowToast($"Recording saved: {_recordingDuration.ToString(@"hh\:mm\:ss")}");
            Log.Information("Recording stopped, duration: {Duration}", _recordingDuration);
            UpdateStorageStatus();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to stop recording");
        }
    }

    private void PauseButton_Click(object sender, RoutedEventArgs e)
    {
        if (_isRecording)
        {
            _isPaused = !_isPaused;
            PauseButton.Content = _isPaused ? "Resume" : "Pause";
            RecordingTimeText.Text = _isPaused ? $"{_recordingDuration:hh\\:mm\\:ss} (Paused)" : _recordingDuration.ToString(@"hh\:mm\:ss");
        }
    }

    private void AutoExposure_Changed(object sender, RoutedEventArgs e)
    {
        _cameraControls.IsAutoExposure = AutoExposureCheck.IsChecked ?? true;
        ExposureSlider.IsEnabled = !(AutoExposureCheck.IsChecked ?? true);
    }

    private void AutoFocus_Changed(object sender, RoutedEventArgs e)
    {
        _cameraControls.IsAutoFocus = AutoFocusCheck.IsChecked ?? true;
        FocusSlider.IsEnabled = !(AutoFocusCheck.IsChecked ?? true);
    }

    private void AutoWB_Changed(object sender, RoutedEventArgs e)
    {
        _cameraControls.IsAutoWhiteBalance = AutoWBCheck.IsChecked ?? true;
    }

    private void ExposureSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        _cameraControls.Exposure = e.NewValue;
        ExposureValueText.Text = ((int)e.NewValue).ToString();
    }

    private void FocusSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        _cameraControls.Focus = e.NewValue / 100.0;
    }

    private void BrightnessSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        _cameraControls.Brightness = e.NewValue;
    }

    private void ContrastSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        _cameraControls.Contrast = e.NewValue;
    }

    private void SaturationSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        _cameraControls.Saturation = e.NewValue;
    }

    private void WBPreset_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (WBPresetSelector.SelectedIndex >= 0)
        {
            var presets = new[] { 0, 4000, 5500, 6500, 2700, 5500 };
            var preset = presets[WBPresetSelector.SelectedIndex];
            if (WBPresetSelector.SelectedIndex == 0)
            {
                _cameraControls.IsAutoWhiteBalance = true;
            }
            else
            {
                _cameraControls.IsAutoWhiteBalance = false;
                _cameraControls.WhiteBalance = preset;
            }
        }
    }

    private void FilterButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is string filterName)
        {
            var filterType = filterName switch
            {
                "Grayscale" => FilterType.Grayscale,
                "Sepia" => FilterType.Sepia,
                "Negative" => FilterType.Negative,
                "Warm" => FilterType.Warm,
                "Cool" => FilterType.Cool,
                "Blur" => FilterType.Blur,
                "Sharpen" => FilterType.Sharpen,
                "Vignette" => FilterType.Vignette,
                _ => FilterType.None
            };
            
            _filterPipeline.AddFilter(filterType, FilterIntensitySlider.Value / 100.0);
            UpdateFilterList();
            ShowToast($"Filter added: {filterName}");
        }
    }

    private void AddFilter_Click(object sender, RoutedEventArgs e)
    {
        ShowToast("Select a filter from available filters");
    }

    private void RemoveFilter_Click(object sender, RoutedEventArgs e)
    {
        if (ActiveFiltersList.SelectedIndex > 0)
        {
            _filterPipeline.Pipeline.RemoveFilter(ActiveFiltersList.SelectedIndex - 1);
            UpdateFilterList();
        }
    }

    private void UpdateFilterList()
    {
        ActiveFiltersList.Items.Clear();
        ActiveFiltersList.Items.Add(new ListBoxItem { Content = "None", IsSelected = true });
        
        var filters = _filterPipeline.Pipeline.Filters;
        foreach (var filter in filters)
        {
            ActiveFiltersList.Items.Add(new ListBoxItem { Content = filter.Name });
        }
    }

    private void LayoutMode_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        var isPip = LayoutModeSelector.SelectedIndex == 3;
        PipPositionLabel.Visibility = isPip ? Visibility.Visible : Visibility.Collapsed;
        PipPositionSelector.Visibility = isPip ? Visibility.Visible : Visibility.Collapsed;
        PipSizeLabel.Visibility = isPip ? Visibility.Visible : Visibility.Collapsed;
        PipSizeSlider.Visibility = isPip ? Visibility.Visible : Visibility.Collapsed;
    }

    private void PipPosition_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
    }

    private void SavePreset_Click(object sender, RoutedEventArgs e)
    {
        var presetName = $"Preset_{DateTime.Now:yyyyMMdd_HHmmss}";
        _cameraControls.SaveCurrentAsPreset(presetName);
        ShowToast($"Preset saved: {presetName}");
    }

    private void LoadPreset_Click(object sender, RoutedEventArgs e)
    {
        ShowToast("Select a preset from dropdown");
    }

    private void ZoomIn_Click(object sender, RoutedEventArgs e)
    {
        _zoomLevel = Math.Min(_zoomLevel + 0.25, 4.0);
        ZoomLevelText.Text = $"{(int)(_zoomLevel * 100)}%";
    }

    private void ZoomOut_Click(object sender, RoutedEventArgs e)
    {
        _zoomLevel = Math.Max(_zoomLevel - 0.25, 0.5);
        ZoomLevelText.Text = $"{(int)(_zoomLevel * 100)}%";
    }

    private void Filters_Click(object sender, RoutedEventArgs e)
    {
        _filtersPanelVisible = !_filtersPanelVisible;
        var filtersExpander = FindExpanderByHeader(SidePanel, "Filters");
        if (filtersExpander != null)
            filtersExpander.IsExpanded = _filtersPanelVisible;
        ShowToast(_filtersPanelVisible ? "Filters panel visible" : "Filters panel hidden");
    }

    private void Controls_Click(object sender, RoutedEventArgs e)
    {
        _controlsPanelVisible = !_controlsPanelVisible;
        var controlsExpander = FindExpanderByHeader(SidePanel, "Camera Controls");
        if (controlsExpander != null)
            controlsExpander.IsExpanded = _controlsPanelVisible;
        ShowToast(_controlsPanelVisible ? "Controls panel visible" : "Controls panel hidden");
    }

    private Expander? FindExpanderByHeader(Panel panel, string header)
    {
        foreach (var child in panel.Children)
        {
            if (child is Expander expander && expander.Header?.ToString() == header)
                return expander;
            if (child is Panel p)
            {
                var found = FindExpanderByHeader(p, header);
                if (found != null) return found;
            }
        }
        return null;
    }

    private void GridToggle_Click(object sender, RoutedEventArgs e)
    {
        _showGrid = !_showGrid;
        PreviewContainer.ShowGridLines = _showGrid;
        ShowToast(_showGrid ? "Grid enabled" : "Grid disabled");
    }

    private void Fullscreen_Click(object sender, RoutedEventArgs e)
    {
        if (_isFullscreen)
        {
            WindowStyle = WindowStyle.SingleBorderWindow;
            WindowState = WindowState.Normal;
            _isFullscreen = false;
            ShowToast("Exited fullscreen");
        }
        else
        {
            WindowStyle = WindowStyle.None;
            WindowState = WindowState.Maximized;
            _isFullscreen = true;
            ShowToast("Fullscreen mode");
        }
    }

    private void RefreshCameras_Click(object sender, RoutedEventArgs e)
    {
        InitializeCameraSelector();
        ShowToast("Cameras refreshed");
    }

    private void StartPreview_Click(object sender, RoutedEventArgs e)
    {
        if (CameraSelector.SelectedIndex > 0 && CameraSelector.SelectedItem is ComboBoxItem item && item.Tag is string cameraId)
        {
            _cameraManager.StartPreview(cameraId);
            _previewTimer?.Start();
            ShowToast("Preview started");
        }
    }

    private void StopPreview_Click(object sender, RoutedEventArgs e)
    {
        _cameraManager.StopPreview();
        _previewTimer?.Stop();
        PreviewImage.Source = null;
        PreviewStatusText.Text = "No Preview";
        ShowToast("Preview stopped");
    }

    private void NewSession_Click(object sender, RoutedEventArgs e) => ShowToast("New session");
    private void Open_Click(object sender, RoutedEventArgs e) => ShowToast("Open file");
    private void Save_Click(object sender, RoutedEventArgs e) => ShowToast("Save");
    private void Exit_Click(object sender, RoutedEventArgs e) => Close();
    private void Preferences_Click(object sender, RoutedEventArgs e) => ShowToast("Preferences");
    private void LayoutSingle_Click(object sender, RoutedEventArgs e) => LayoutModeSelector.SelectedIndex = 0;
    private void Layout2x2_Click(object sender, RoutedEventArgs e) => LayoutModeSelector.SelectedIndex = 1;
    private void LayoutPip_Click(object sender, RoutedEventArgs e) => LayoutModeSelector.SelectedIndex = 3;
    private void Settings_Click(object sender, RoutedEventArgs e) => ShowToast("Settings");
    private void Storage_Click(object sender, RoutedEventArgs e) => ShowToast("Storage");
    private void About_Click(object sender, RoutedEventArgs e) => MessageBox.Show("Camera Pro v1.0\nAdvanced Windows Camera Application", "About", MessageBoxButton.OK);

    private void Window_KeyDown(object sender, KeyEventArgs e)
    {
        switch (e.Key)
        {
            case Key.Space:
                PhotoButton_Click(sender, e);
                e.Handled = true;
                break;
            case Key.R:
                RecordButton_Click(sender, e);
                e.Handled = true;
                break;
            case Key.P:
                if (_isRecording) PauseButton_Click(sender, e);
                e.Handled = true;
                break;
            case Key.F:
            case Key.F11:
                Fullscreen_Click(sender, e);
                e.Handled = true;
                break;
            case Key.Escape:
                if (_isFullscreen)
                {
                    WindowStyle = WindowStyle.SingleBorderWindow;
                    WindowState = WindowState.Normal;
                    _isFullscreen = false;
                }
                e.Handled = true;
                break;
            case Key.G:
                GridToggle_Click(sender, e);
                e.Handled = true;
                break;
            case Key.D1: case Key.NumPad1:
                ApplyQuickFilter(0); e.Handled = true; break;
            case Key.D2: case Key.NumPad2:
                ApplyQuickFilter(1); e.Handled = true; break;
            case Key.D3: case Key.NumPad3:
                ApplyQuickFilter(2); e.Handled = true; break;
            case Key.D4: case Key.NumPad4:
                ApplyQuickFilter(3); e.Handled = true; break;
            case Key.D5: case Key.NumPad5:
                ApplyQuickFilter(4); e.Handled = true; break;
            case Key.D6: case Key.NumPad6:
                ApplyQuickFilter(5); e.Handled = true; break;
            case Key.D7: case Key.NumPad7:
                ApplyQuickFilter(6); e.Handled = true; break;
            case Key.D8: case Key.NumPad8:
                ApplyQuickFilter(7); e.Handled = true; break;
            case Key.D9: case Key.NumPad9:
                ApplyQuickFilter(8); e.Handled = true; break;
        }
    }

    private void ApplyQuickFilter(int index)
    {
        var filters = new[] { "Grayscale", "Sepia", "Negative", "Warm", "Cool", "Blur", "Sharpen", "Vignette" };
        if (index < filters.Length)
        {
            FilterButton_Click(new Button { Tag = filters[index] }, new RoutedEventArgs());
        }
    }

    private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
    {
        _previewTimer?.Stop();
        _recordingTimer?.Stop();
        _countdownTimer?.Stop();
        _cameraManager.StopPreview();
        
        if (_isRecording)
        {
            StopRecording();
        }
        
        Log.Information("MainWindow closing");
    }

    private void UpdateStorageStatus()
    {
        try
        {
            var driveInfo = new DriveInfo(Path.GetPathRoot(Environment.GetFolderPath(Environment.SpecialFolder.MyPictures)) ?? "C:");
            var freeSpace = driveInfo.AvailableFreeSpace / (1024.0 * 1024.0 * 1024.0);
            StorageStatusText.Text = $"Free: {freeSpace:F1} GB";
        }
        catch
        {
            StorageStatusText.Text = "Free: -- GB";
        }
    }

    private static TextBlock? _toast;
    private static DispatcherTimer? _toastTimer;
    
    private void ShowToast(string message)
    {
        if (_toast == null)
        {
            _toast = new TextBlock
            {
                Foreground = Brushes.White,
                Background = new SolidColorBrush(Color.FromArgb(200, 0, 0, 0)),
                Padding = new Thickness(16, 8, 16, 8),
                FontSize = 14,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Bottom,
                Margin = new Thickness(0, 0, 0, 60),
                Opacity = 0
            };
            
            var grid = (Grid)Content;
            grid.Children.Add(_toast);
        }
        
        _toast.Text = message;
        _toast.Opacity = 1;
        
        _toastTimer?.Stop();
        _toastTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(3) };
        _toastTimer.Tick += (s, args) =>
        {
            if (_toast != null) _toast.Opacity = 0;
            _toastTimer.Stop();
        };
        _toastTimer.Start();
    }
}