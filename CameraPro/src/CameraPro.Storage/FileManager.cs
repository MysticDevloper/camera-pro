using CameraPro.Core.Interfaces;
using Serilog;
using System.IO;
using System.Security.Cryptography;

namespace CameraPro.Storage;

public class FileManager : IFileManager
{
    private readonly string _basePhotoPath;
    private readonly string _baseVideoPath;
    private readonly Dictionary<string, FileMetadata> _fileCache = new();
    private readonly ILogger _logger;

    public FileManager()
    {
        _basePhotoPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyPictures), "CameraPro");
        _baseVideoPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyVideos), "CameraPro");
        _logger = Log.ForContext<FileManager>();

        EnsureDirectoryExists(_basePhotoPath);
        EnsureDirectoryExists(_baseVideoPath);
    }

    public async Task<List<MediaFile>> GetMediaFilesAsync(string folderPath, MediaFilter? filter = null, bool recursive = true)
    {
        var files = new List<MediaFile>();

        try
        {
            if (!Directory.Exists(folderPath))
            {
                _logger.Warning("Folder does not exist: {FolderPath}", folderPath);
                return files;
            }

            var searchPattern = filter switch
            {
                MediaFilter.Photos => "*.jpg|*.jpeg|*.png|*.bmp",
                MediaFilter.Videos => "*.mp4|*.mkv|*.avi|*.webm",
                _ => "*.jpg|*.jpeg|*.png|*.bmp|*.mp4|*.mkv|*.avi|*.webm"
            };

            var searchOption = recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
            var patterns = searchPattern.Split('|');
            foreach (var pattern in patterns)
            {
                var foundFiles = Directory.GetFiles(folderPath, pattern, searchOption);
                foreach (var file in foundFiles)
                {
                    var mediaFile = await CreateMediaFileAsync(file);
                    if (mediaFile != null)
                    {
                        files.Add(mediaFile);
                    }
                }
            }

            _logger.Information("Found {Count} media files in {FolderPath}", files.Count, folderPath);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error getting media files from {FolderPath}", folderPath);
        }

        return files.OrderByDescending(f => f.ModifiedAt).ToList();
    }

    public async Task<bool> DeleteFileAsync(string fileId)
    {
        try
        {
            var file = await GetFileInfoAsync(fileId);
            if (string.IsNullOrEmpty(file.FullPath) || !File.Exists(file.FullPath))
            {
                _logger.Warning("File not found: {FileId}", fileId);
                return false;
            }

            await Task.Run(() => File.Delete(file.FullPath));
            _fileCache.Remove(fileId);
            _logger.Information("Deleted file: {FilePath}", file.FullPath);
            return true;
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error deleting file: {FileId}", fileId);
            return false;
        }
    }

    public async Task<bool> MoveFileAsync(string fileId, string destination)
    {
        try
        {
            var file = await GetFileInfoAsync(fileId);
            if (string.IsNullOrEmpty(file.FullPath) || !File.Exists(file.FullPath))
            {
                _logger.Warning("File not found: {FileId}", fileId);
                return false;
            }

            EnsureDirectoryExists(destination);
            var destPath = Path.Combine(destination, file.Name);
            await Task.Run(() => File.Move(file.FullPath, destPath));
            _fileCache[fileId] = new FileMetadata
            {
                Id = fileId,
                Name = file.Name,
                FullPath = destPath,
                SizeBytes = file.SizeBytes,
                CreatedAt = file.CreatedAt,
                ModifiedAt = DateTime.Now
            };
            _logger.Information("Moved file from {Source} to {Destination}", file.FullPath, destPath);
            return true;
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error moving file: {FileId}", fileId);
            return false;
        }
    }

    public async Task<bool> CopyFileAsync(string fileId, string destination)
    {
        try
        {
            var file = await GetFileInfoAsync(fileId);
            if (string.IsNullOrEmpty(file.FullPath) || !File.Exists(file.FullPath))
            {
                _logger.Warning("File not found: {FileId}", fileId);
                return false;
            }

            EnsureDirectoryExists(destination);
            var destPath = Path.Combine(destination, file.Name);
            await Task.Run(() => File.Copy(file.FullPath, destPath));
            _logger.Information("Copied file from {Source} to {Destination}", file.FullPath, destPath);
            return true;
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error copying file: {FileId}", fileId);
            return false;
        }
    }

    public async Task<FileMetadata> GetFileInfoAsync(string fileId)
    {
        if (_fileCache.TryGetValue(fileId, out var cached))
        {
            return cached;
        }

        try
        {
            if (!File.Exists(fileId))
            {
                return new FileMetadata { Id = fileId };
            }

            var fileInfo = new FileInfo(fileId);
            var metadata = new FileMetadata
            {
                Id = fileId,
                Name = fileInfo.Name,
                FullPath = fileInfo.FullName,
                SizeBytes = fileInfo.Length,
                CreatedAt = fileInfo.CreationTime,
                ModifiedAt = fileInfo.LastWriteTime,
                IsReadOnly = fileInfo.IsReadOnly,
                Extension = fileInfo.Extension
            };

            _fileCache[fileId] = metadata;
            return metadata;
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error getting file info: {FileId}", fileId);
            return new FileMetadata { Id = fileId };
        }
    }

    public string GetMediaPath(MediaType type, DateTime date)
    {
        var basePath = type == MediaType.Photo ? _basePhotoPath : _baseVideoPath;
        var folderPath = Path.Combine(basePath, date.Year.ToString(), $"{date.Month:D2}-{date:MMMM}");

        EnsureDirectoryExists(folderPath);
        return folderPath;
    }

    private async Task<MediaFile?> CreateMediaFileAsync(string filePath)
    {
        try
        {
            var fileInfo = new FileInfo(filePath);
            var extension = fileInfo.Extension.ToLowerInvariant();
            var isVideo = extension is ".mp4" or ".mkv" or ".avi" or ".webm";

            var mediaFile = new MediaFile
            {
                Id = ComputeFileId(filePath),
                Name = fileInfo.Name,
                FullPath = filePath,
                Type = isVideo ? MediaType.Video : MediaType.Photo,
                SizeBytes = fileInfo.Length,
                CreatedAt = fileInfo.CreationTime,
                ModifiedAt = fileInfo.LastWriteTime
            };

            return await Task.FromResult(mediaFile);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error creating media file: {FilePath}", filePath);
            return null;
        }
    }

    private string ComputeFileId(string filePath)
    {
        using var md5 = MD5.Create();
        var hash = md5.ComputeHash(System.Text.Encoding.UTF8.GetBytes(filePath));
        return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
    }

    private void EnsureDirectoryExists(string path)
    {
        if (!Directory.Exists(path))
        {
            Directory.CreateDirectory(path);
            _logger.Information("Created directory: {Path}", path);
        }
    }
}
