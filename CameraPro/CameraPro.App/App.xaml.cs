using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using CameraPro.Core;
using CameraPro.Core.Interfaces;
using CameraPro.Core.Infrastructure;
using CameraPro.Camera;
using CameraPro.Capture;
using CameraPro.Controls;
using CameraPro.Filters;
using CameraPro.Recording;
using CameraPro.MultiCamera;
using CameraPro.Storage;
using Serilog;

namespace CameraPro.App;

public partial class App : Application
{
    public static IServiceProvider Services { get; private set; } = null!;

    protected override void OnStartup(StartupEventArgs e)
    {
        Services = ServiceConfiguration.ConfigureServices();
        
        LoggingSetup.Configure();
        
        AppDomain.CurrentDomain.UnhandledException += GlobalExceptionHandler.Handle;
        
        base.OnStartup(e);
        
        var mainWindow = new MainWindow();
        mainWindow.Show();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        LoggingSetup.CloseAndFlush();
        base.OnExit(e);
    }
}