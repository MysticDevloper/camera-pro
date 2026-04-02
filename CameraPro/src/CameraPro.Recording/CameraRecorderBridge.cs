using CameraPro.Core.Interfaces;
using CameraPro.Core.Models;
using Serilog;
using System.IO;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace CameraPro.Recording;

public class CameraRecorderBridge : IDisposable
{
    private readonly ICameraManager _cameraManager;
    private readonly IVideoRecorder _videoRecorder;
    private readonly ILogger _logger;
    private bool _isRecording;

    public CameraRecorderBridge(ICameraManager cameraManager, IVideoRecorder videoRecorder)
    {
        _cameraManager = cameraManager;
        _videoRecorder = videoRecorder;
        _logger = Log.ForContext<CameraRecorderBridge>();
        
        _cameraManager.FrameAvailable += OnFrameAvailable;
    }

    private void OnFrameAvailable(object? sender, BitmapSource frame)
    {
        if (!_isRecording || !_videoRecorder.IsRecording)
            return;

        try
        {
            var frameData = ConvertBitmapSourceToYuv(frame);
            if (frameData != null)
            {
                _videoRecorder.WriteFrame(frameData);
            }
        }
        catch (Exception ex)
        {
            _logger.Warning(ex, "Error writing frame to recorder");
        }
    }

    private byte[]? ConvertBitmapSourceToYuv(BitmapSource frame)
    {
        try
        {
            var formatted = new FormatConvertedBitmap();
            formatted.BeginInit();
            formatted.Source = frame;
            formatted.DestinationFormat = PixelFormats.Bgra32;
            formatted.EndInit();
            formatted.Freeze();

            int width = formatted.PixelWidth;
            int height = formatted.PixelHeight;
            int stride = width * 4;
            byte[] pixels = new byte[height * stride];

            formatted.CopyPixels(pixels, stride, 0);

            return ConvertBgraToYuv420(pixels, width, height);
        }
        catch (Exception ex)
        {
            _logger.Warning(ex, "Failed to convert frame");
            return null;
        }
    }

    private byte[] ConvertBgraToYuv420(byte[] bgra, int width, int height)
    {
        int ySize = width * height;
        int uvSize = ySize / 4;
        byte[] yuv = new byte[ySize + uvSize * 2];

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                int idx = y * width * 4 + x * 4;
                byte b = bgra[idx];
                byte g = bgra[idx + 1];
                byte r = bgra[idx + 2];

                int yVal = ((66 * r + 129 * g + 25 * b + 128) >> 8) + 16;
                yuv[y * width + x] = (byte)Math.Clamp(yVal, 0, 255);
            }
        }

        for (int y = 0; y < height; y += 2)
        {
            for (int x = 0; x < width; x += 2)
            {
                int idx = y * width * 4 + x * 4;
                byte b = bgra[idx];
                byte g = bgra[idx + 1];
                byte r = bgra[idx + 2];

                int uVal = ((-38 * r - 74 * g + 112 * b + 128) >> 8) + 128;
                int vVal = ((112 * r - 94 * g - 18 * b + 128) >> 8) + 128;

                int uvIdx = ySize + (y / 2) * width + x;
                yuv[uvIdx] = (byte)Math.Clamp(uVal, 0, 255);
                yuv[uvIdx + 1] = (byte)Math.Clamp(vVal, 0, 255);
            }
        }

        return yuv;
    }

    public async Task<bool> StartRecordingAsync(string outputPath, CaptureSettings settings)
    {
        try
        {
            _isRecording = true;
            _videoRecorder.StartRecording(outputPath, settings);
            _logger.Information("Recording started: {Path}", outputPath);
            return true;
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Failed to start recording");
            _isRecording = false;
            return false;
        }
    }

    public RecordingSession StopRecording()
    {
        _isRecording = false;
        var session = _videoRecorder.StopRecording();
        _logger.Information("Recording stopped: {Path}", session.FilePath);
        return session;
    }

    public void Dispose()
    {
        _cameraManager.FrameAvailable -= OnFrameAvailable;
        if (_isRecording)
        {
            StopRecording();
        }
    }
}