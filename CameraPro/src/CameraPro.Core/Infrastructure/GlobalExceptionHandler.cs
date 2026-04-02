using System.Windows;
using Serilog;

namespace CameraPro.Core.Infrastructure;

public static class GlobalExceptionHandler
{
    public static void Setup()
    {
        AppDomain.CurrentDomain.UnhandledException += (s, e) =>
        {
            var ex = e.ExceptionObject as Exception;
            Log.Fatal(ex, "Unhandled domain exception");
            MessageBox.Show($"A fatal error occurred: {ex?.Message}", "Camera Pro - Error", MessageBoxButton.OK, MessageBoxImage.Error);
            Environment.Exit(1);
        };

        Application.Current.DispatcherUnhandledException += (s, e) =>
        {
            Log.Error(e.Exception, "Unhandled UI exception");
            MessageBox.Show($"An error occurred: {e.Exception.Message}", "Camera Pro - Error", MessageBoxButton.OK, MessageBoxImage.Error);
            e.Handled = true;
        };

        TaskScheduler.UnobservedTaskException += (s, e) =>
        {
            Log.Error(e.Exception, "Unobserved task exception");
            e.SetObserved();
        };
    }
}
