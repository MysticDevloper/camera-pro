using CameraPro.Core.Enums;

namespace CameraPro.Core.Interfaces;

public interface IFileManager
{
    Task<List<MediaFile>> GetMediaFilesAsync(string folderPath, MediaFilter? filter = null, bool recursive = true);
    Task<bool> DeleteFileAsync(string fileId);
    Task<bool> MoveFileAsync(string fileId, string destination);
    Task<bool> CopyFileAsync(string fileId, string destination);
    Task<FileMetadata> GetFileInfoAsync(string fileId);
    string GetMediaPath(MediaType type, DateTime date);
}

public interface IMediaLibrary
{
    Task ScanFolderAsync(string path, bool recursive = true);
    Task ScanAllMediaFoldersAsync();
    Task<List<MediaFile>> SearchAsync(string query, MediaType? type = null);
    Task<List<MediaFile>> GetRecentFilesAsync(int count = 20);
    Task<byte[]?> GetThumbnailAsync(string fileId);
    Task RegenerateThumbnailsAsync();
    event EventHandler<MediaFile>? FileAdded;
    event EventHandler<MediaFile>? FileRemoved;
}

public interface IVideoConverter
{
    Task<string> ConvertAsync(string inputPath, string outputPath, VideoFormat targetFormat, ConversionQuality quality = ConversionQuality.Medium, IProgress<int>? progress = null);
    Task<string> CompressAsync(string inputPath, string outputPath, int targetBitrate, IProgress<int>? progress = null);
    Task<string> TrimAsync(string inputPath, string outputPath, TimeSpan start, TimeSpan duration);
    Task<string> MergeAsync(List<string> inputPaths, string outputPath);
    Task<string> ChangeResolutionAsync(string inputPath, string outputPath, int width, int height);
}

public interface IImageExporter
{
    Task<string> ExportAsync(string inputPath, string outputPath, ImageFormat format, int quality = 90);
    Task<List<string>> BatchResizeAsync(List<string> inputPaths, string outputFolder, int maxWidth, int maxHeight);
    Task<List<string>> BatchConvertAsync(List<string> inputPaths, string outputFolder, ImageFormat format);
    Task<string> ApplyWatermarkAsync(string inputPath, string outputPath, string watermarkPath, WatermarkPosition position);
    Task BatchRenameAsync(List<string> fileIds, string template);
}

public interface IStorageStatistics
{
    Task<StorageInfo> GetStorageInfoAsync(string rootPath);
    Task<List<FileSizeInfo>> GetLargestFilesAsync(int count = 10);
    Task<List<FileDateInfo>> GetOldestFilesAsync(int count = 10);
    Task<List<DuplicateInfo>> FindDuplicatesAsync();
}

public enum MediaType
{
    Photo,
    Video
}

public enum MediaFilter
{
    All,
    Photos,
    Videos
}

public enum ConversionQuality
{
    Low,
    Medium,
    High,
    Ultra
}

public enum ImageFormat
{
    JPEG,
    PNG,
    BMP,
    TIFF
}

public enum WatermarkPosition
{
    TopLeft,
    TopRight,
    BottomLeft,
    BottomRight,
    Center
}

public class MediaFile
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = string.Empty;
    public string FullPath { get; set; } = string.Empty;
    public MediaType Type { get; set; }
    public long SizeBytes { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime ModifiedAt { get; set; }
    public string? ThumbnailPath { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }
    public TimeSpan? Duration { get; set; }
    public string? CameraName { get; set; }
}

public class FileMetadata
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string FullPath { get; set; } = string.Empty;
    public long SizeBytes { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime ModifiedAt { get; set; }
    public bool IsReadOnly { get; set; }
    public string? Extension { get; set; }
}

public class StorageInfo
{
    public long TotalBytes { get; set; }
    public long UsedBytes { get; set; }
    public long FreeBytes { get; set; }
    public long PhotoBytes { get; set; }
    public long VideoBytes { get; set; }
    public int PhotoCount { get; set; }
    public int VideoCount { get; set; }
}

public class FileSizeInfo
{
    public string FileId { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string FullPath { get; set; } = string.Empty;
    public long SizeBytes { get; set; }
}

public class FileDateInfo
{
    public string FileId { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string FullPath { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

public class DuplicateInfo
{
    public string Hash { get; set; } = string.Empty;
    public List<string> FilePaths { get; set; } = new();
    public long TotalSize { get; set; }
}
