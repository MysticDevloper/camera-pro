using Microsoft.Extensions.DependencyInjection;
using CameraPro.Core.Interfaces;
using CameraPro.Core;
using CameraPro.Camera;
using CameraPro.Recording;
using CameraPro.Capture;
using CameraPro.MultiCamera;
using CameraPro.Controls;
using CameraPro.Filters;
using CameraPro.Storage;

namespace CameraPro.App;

public static class ServiceConfiguration
{
    public static IServiceProvider ConfigureServices()
    {
        var services = new ServiceCollection();
        
        services.AddSingleton<AppConfig>();
        
        services.AddSingleton<ICameraManager, CameraManager>();
        services.AddSingleton<IVideoRecorder, VideoRecorder>();
        services.AddSingleton<IPhotoCapture, PhotoCapture>();
        
        services.AddSingleton<BurstMode>();
        services.AddSingleton<TimerMode>();
        
        services.AddSingleton<CameraControls>();
        
        services.AddSingleton<FilterService>();
        
        services.AddSingleton<RecordingController>();
        services.AddSingleton<CameraRecorderBridge>();
        
        services.AddSingleton<MultiCameraManager>();
        
        services.AddSingleton<IFileManager, FileManager>();
        services.AddSingleton<IMediaLibrary, MediaLibrary>();
        services.AddSingleton<IVideoConverter, VideoConverter>();
        services.AddSingleton<IImageExporter, ImageExporter>();
        services.AddSingleton<IStorageStatistics, StorageStatistics>();
        services.AddSingleton<ThumbnailGenerator>();
        
        services.AddSingleton<EncoderManager>();
        services.AddSingleton<PresetManager>();
        
        return services.BuildServiceProvider();
    }
}