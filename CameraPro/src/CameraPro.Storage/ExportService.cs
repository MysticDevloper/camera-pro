using CameraPro.Core.Interfaces;
using Serilog;
using System.IO;

namespace CameraPro.Storage;

public class ExportService : IExportService
{
    private readonly IFileManager _fileManager;
    private readonly IVideoConverter _videoConverter;
    private readonly IImageExporter _imageExporter;
    private readonly ILogger _logger;

    public ExportService(IFileManager fileManager, IVideoConverter videoConverter, IImageExporter imageExporter)
    {
        _fileManager = fileManager;
        _videoConverter = videoConverter;
        _imageExporter = imageExporter;
        _logger = Log.ForContext<ExportService>();
    }

    public async Task<ExportResult> ExportToLocalAsync(string sourcePath, string destinationPath, ExportOptions options)
    {
        var result = new ExportResult { Success = false };

        try
        {
            var extension = Path.GetExtension(sourcePath).ToLowerInvariant();
            var isVideo = extension is ".mp4" or ".mkv" or ".avi" or ".webm";

            if (isVideo && options.ConvertFormat.HasValue)
            {
                var outputPath = await _videoConverter.ConvertAsync(
                    sourcePath,
                    Path.ChangeExtension(destinationPath, options.ConvertFormat.Value.ToString().ToLowerInvariant()),
                    options.ConvertFormat.Value,
                    options.Quality ?? ConversionQuality.Medium);

                result.OutputPath = outputPath;
            }
            else if (!isVideo && options.ImageFormat.HasValue)
            {
                var outputPath = await _imageExporter.ExportAsync(
                    sourcePath,
                    Path.ChangeExtension(destinationPath, options.ImageFormat.Value.ToString().ToLowerInvariant()),
                    options.ImageFormat.Value,
                    options.Quality ?? 90);

                result.OutputPath = outputPath;
            }
            else
            {
                var destDir = Path.GetDirectoryName(destinationPath) ?? string.Empty;
                if (!Directory.Exists(destDir))
                {
                    Directory.CreateDirectory(destDir);
                }

                File.Copy(sourcePath, destinationPath, true);
                result.OutputPath = destinationPath;
            }

            result.Success = true;
            _logger.Information("Exported {Source} to {Destination}", sourcePath, destinationPath);
        }
        catch (Exception ex)
        {
            result.ErrorMessage = ex.Message;
            _logger.Error(ex, "Export failed: {Source}", sourcePath);
        }

        return result;
    }

    public async Task<ExportResult> UploadToCloudAsync(string filePath, CloudProvider provider, string destinationName)
    {
        var result = new ExportResult { Success = false };

        try
        {
            result.Success = provider switch
            {
                CloudProvider.FTP => await UploadToFtpAsync(filePath, destinationName),
                CloudProvider.Local => await CopyToLocalAsync(filePath, destinationName),
                _ => false
            };

            if (result.Success)
            {
                result.OutputPath = destinationName;
                _logger.Information("Uploaded {File} to {Provider}", filePath, provider);
            }
        }
        catch (Exception ex)
        {
            result.ErrorMessage = ex.Message;
            _logger.Error(ex, "Upload failed: {File}", filePath);
        }

        return result;
    }

    private async Task<bool> UploadToFtpAsync(string filePath, string destination)
    {
        _logger.Information("FTP upload not configured - using local copy");
        return await Task.FromResult(false);
    }

    private async Task<bool> CopyToLocalAsync(string filePath, string destination)
    {
        try
        {
            var destDir = Path.GetDirectoryName(destination) ?? string.Empty;
            if (!Directory.Exists(destDir))
            {
                Directory.CreateDirectory(destDir);
            }

            File.Copy(filePath, destination, true);
            return true;
        }
        catch
        {
            return false;
        }
    }
}

public interface IExportService
{
    Task<ExportResult> ExportToLocalAsync(string sourcePath, string destinationPath, ExportOptions options);
    Task<ExportResult> UploadToCloudAsync(string filePath, CloudProvider provider, string destinationName);
}

public class ExportOptions
{
    public VideoFormat? ConvertFormat { get; set; }
    public ImageFormat? ImageFormat { get; set; }
    public object? Quality { get; set; }
    public bool PreserveMetadata { get; set; } = true;
}

public class ExportResult
{
    public bool Success { get; set; }
    public string? OutputPath { get; set; }
    public string? ErrorMessage { get; set; }
}

public enum CloudProvider
{
    FTP,
    SFTP,
    Local
}
