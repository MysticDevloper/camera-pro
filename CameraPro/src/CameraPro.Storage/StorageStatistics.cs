using CameraPro.Core.Interfaces;
using Serilog;
using System.IO;
using System.Security.Cryptography;

namespace CameraPro.Storage;

public class StorageStatistics : IStorageStatistics
{
    private readonly IFileManager _fileManager;
    private readonly ILogger _logger;

    public StorageStatistics(IFileManager fileManager)
    {
        _fileManager = fileManager;
        _logger = Log.ForContext<StorageStatistics>();
    }

    public async Task<StorageInfo> GetStorageInfoAsync(string rootPath)
    {
        var info = new StorageInfo();

        try
        {
            var drive = new DriveInfo(Path.GetPathRoot(rootPath) ?? "C");
            info.TotalBytes = drive.TotalSize;
            info.FreeBytes = drive.AvailableFreeSpace;
            info.UsedBytes = info.TotalBytes - info.FreeBytes;

            var files = await _fileManager.GetMediaFilesAsync(rootPath);
            info.PhotoCount = files.Count(f => f.Type == MediaType.Photo);
            info.VideoCount = files.Count(f => f.Type == MediaType.Video);
            info.PhotoBytes = files.Where(f => f.Type == MediaType.Photo).Sum(f => f.SizeBytes);
            info.VideoBytes = files.Where(f => f.Type == MediaType.Video).Sum(f => f.SizeBytes);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error getting storage info for: {Path}", rootPath);
        }

        return info;
    }

    public async Task<List<FileSizeInfo>> GetLargestFilesAsync(int count = 10)
    {
        var rootPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyPictures), "CameraPro");
        var files = await _fileManager.GetMediaFilesAsync(rootPath);

        return files
            .OrderByDescending(f => f.SizeBytes)
            .Take(count)
            .Select(f => new FileSizeInfo
            {
                FileId = f.Id,
                FileName = f.Name,
                FullPath = f.FullPath,
                SizeBytes = f.SizeBytes
            })
            .ToList();
    }

    public async Task<List<FileDateInfo>> GetOldestFilesAsync(int count = 10)
    {
        var rootPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyPictures), "CameraPro");
        var files = await _fileManager.GetMediaFilesAsync(rootPath);

        return files
            .OrderBy(f => f.CreatedAt)
            .Take(count)
            .Select(f => new FileDateInfo
            {
                FileId = f.Id,
                FileName = f.Name,
                FullPath = f.FullPath,
                CreatedAt = f.CreatedAt
            })
            .ToList();
    }

    public async Task<List<DuplicateInfo>> FindDuplicatesAsync()
    {
        var photoPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyPictures), "CameraPro");
        var videoPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyVideos), "CameraPro");

        var allFiles = new List<string>();
        if (Directory.Exists(photoPath))
            allFiles.AddRange(Directory.GetFiles(photoPath, "*.*", SearchOption.AllDirectories));
        if (Directory.Exists(videoPath))
            allFiles.AddRange(Directory.GetFiles(videoPath, "*.*", SearchOption.AllDirectories));

        var hashGroups = new Dictionary<string, List<string>>();

        foreach (var file in allFiles)
        {
            try
            {
                var hash = await ComputeFileHashAsync(file);
                if (!hashGroups.ContainsKey(hash))
                {
                    hashGroups[hash] = new List<string>();
                }
                hashGroups[hash].Add(file);
            }
            catch (Exception ex)
            {
                _logger.Warning(ex, "Error hashing file: {Path}", file);
            }
        }

        return hashGroups
            .Where(g => g.Value.Count > 1)
            .Select(g => new DuplicateInfo
            {
                Hash = g.Key,
                FilePaths = g.Value,
                TotalSize = g.Value.Sum(f => new FileInfo(f).Length)
            })
            .ToList();
    }

    private async Task<string> ComputeFileHashAsync(string filePath)
    {
        using var md5 = MD5.Create();
        using var stream = File.OpenRead(filePath);
        var hash = await Task.Run(() => md5.ComputeHash(stream));
        return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
    }
}
