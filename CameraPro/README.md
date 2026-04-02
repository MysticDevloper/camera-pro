# Camera Pro - Advanced Windows Camera Application

A feature-rich Windows camera application built with C# and .NET 8 using WPF.

## Features

- **Real-time Camera Preview** - Live feed from any connected camera
- **Multi-Camera Support** - Switch between cameras, view multiple simultaneously
- **Video Recording** - Record in MP4, MKV, AVI, WEBM formats with H.264/H.265 codecs
- **Photo Capture** - Single shot, burst mode, and timer mode
- **Camera Controls** - Exposure, focus, white balance, brightness, contrast, saturation
- **Filters & Effects** - Grayscale, sepia, warm, cool, blur, sharpen, vignette, and more
- **Face Detection** - Automatic face detection and tracking
- **Text/Watermark Overlay** - Add custom text, timestamps, and logos
- **Storage Management** - Auto-organize files by date, media library, conversion tools

## System Requirements

- Windows 10 version 1903 (build 18362) or later
- .NET 8.0 Runtime (included in self-contained build)
- Webcam or video capture device
- 4GB RAM minimum
- 500MB disk space

## Installation

1. Download the latest release from the `publish` folder
2. Run `CameraPro.exe`
3. Grant camera permissions when prompted

## Keyboard Shortcuts

| Key | Action |
|-----|--------|
| Space | Capture photo |
| R | Toggle recording |
| P | Pause/Resume recording |
| F11 | Fullscreen toggle |
| G | Toggle grid |
| 1-9 | Quick filter selection |
| Escape | Cancel/Close |

## Usage

### Starting Preview
1. Select camera from dropdown
2. Choose resolution (4K, 1080p, 720p)
3. Select frame rate (15, 30, 60 fps)
4. Click "Start Preview"

### Recording Video
1. Configure recording settings
2. Click the red record button
3. Click again to stop
4. Video saved to configured folder

### Taking Photos
1. Select photo format (JPEG, PNG, BMP)
2. Configure timer/burst mode (optional)
3. Click the capture button

### Applying Filters
1. Expand Filters panel
2. Click filter buttons to apply
3. Adjust intensity with slider
4. Filters apply in real-time

## Troubleshooting

### Camera Not Detected
- Check Device Manager for camera drivers
- Ensure camera is not in use by another app
- Restart the application

### Recording Issues
- Check available disk space
- Verify write permissions
- Try lower resolution

### Performance Issues
- Close other camera applications
- Update graphics drivers
- Reduce filter usage

## Project Structure

```
CameraPro/
├── CameraPro.sln
├── CameraPro.App/
│   └── MainWindow, App configuration
├── src/
│   ├── CameraPro.Core/       - Models, interfaces, enums
│   ├── CameraPro.Camera/    - Camera management
│   ├── CameraPro.Recording/ - Video recording
│   ├── CameraPro.Capture/   - Photo capture
│   ├── CameraPro.MultiCamera/ - Multi-camera support
│   ├── CameraPro.Controls/  - Camera controls
│   ├── CameraPro.Filters/   - Filters & effects
│   └── CameraPro.Storage/   - Storage & export
└── publish/
    └── CameraPro.exe         - Compiled application
```

## Storage Locations

Photos: `C:\Users\[User]\Pictures\CameraPro`
Videos: `C:\Users\[User]\Videos\CameraPro`

## Build from Source

```bash
cd C:/CAMERA/CameraPro
dotnet restore
dotnet build
dotnet publish CameraPro.App/CameraPro.App.csproj -c Release -r win-x64 --self-contained
```

## License

This project is provided as open source for educational and practical use.

---

Built with .NET 8 and WPF