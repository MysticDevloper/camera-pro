using CameraPro.Core.Interfaces;
using Serilog;
using System.IO;

namespace CameraPro.Storage;

public class MediaLibrary : IMediaLibrary
{
    private readonly IFileManager _fileManager;
    private readonly List<MediaFile> _mediaFiles = new();
    private readonly Dictionary<string, byte[]> _thumbnailCache = new();
    private readonly string _thumbnailCachePath;
    private readonly ILogger _logger;

    public event EventHandler<MediaFile>? FileAdded;
    public event EventHandler<MediaFile>? FileRemoved;

    public MediaLibrary(IFileManager fileManager)
    {
        _fileManager = fileManager;
        _thumbnailCachePath = Path.Combine(Path.GetTempPath(), "CameraPro", "Thumbnails");
        _logger = Log.ForContext<MediaLibrary>();

        EnsureDirectoryExists(_thumbnailCachePath);
    }

    public async Task ScanFolderAsync(string path, bool recursive = true)
    {
        try
        {
            _mediaFiles.Clear();
            var files = await _fileManager.GetMediaFilesAsync(path, recursive ? null : MediaFilter.All);

            foreach (var file in files)
            {
                _mediaFiles.Add(file);
                FileAdded?.Invoke(this, file);
            }

            await GenerateThumbnailsAsync();
            _logger.Information("Scanned {Count} files from {Path}", files.Count, path);
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error scanning folder: {Path}", path);
        }
    }

    public async Task ScanAllMediaFoldersAsync()
    {
        var picturesPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyPictures), "CameraPro");
        var videosPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyVideos), "CameraPro");

        if (Directory.Exists(picturesPath))
        {
            await ScanFolderAsync(picturesPath);
        }
        if (Directory.Exists(videosPath))
        {
            await ScanFolderAsync(videosPath);
        }

        _logger.Information("Scanned all media folders");
    }

    public async Task<List<MediaFile>> SearchAsync(string query, MediaType? type = null)
    {
        var normalizedQuery = query.ToLowerInvariant();

        var results = _mediaFiles.Where(f =>
            (string.IsNullOrEmpty(normalizedQuery) || f.Name.ToLowerInvariant().Contains(normalizedQuery)) &&
            (!type.HasValue || f.Type == type.Value)
        ).ToList();

        return await Task.FromResult(results);
    }

    public async Task<List<MediaFile>> GetRecentFilesAsync(int count = 20)
    {
        var recentFiles = _mediaFiles
            .OrderByDescending(f => f.ModifiedAt)
            .Take(count)
            .ToList();

        return await Task.FromResult(recentFiles);
    }

    public async Task<byte[]?> GetThumbnailAsync(string fileId)
    {
        if (_thumbnailCache.TryGetValue(fileId, out var cached))
        {
            return cached;
        }

        var file = _mediaFiles.FirstOrDefault(f => f.Id == fileId);
        if (file == null || !File.Exists(file.FullPath))
        {
            return null;
        }

        try
        {
            var thumbnail = await GenerateThumbnailAsync(file);
            if (thumbnail != null)
            {
                _thumbnailCache[fileId] = thumbnail;
            }
            return thumbnail;
        }
        catch (Exception ex)
        {
            _logger.Error(ex, "Error generating thumbnail for {FileId}", fileId);
            return null;
        }
    }

    public async Task RegenerateThumbnailsAsync()
    {
        _thumbnailCache.Clear();

        foreach (var file in _mediaFiles)
        {
            try
            {
                var thumbnail = await GenerateThumbnailAsync(file);
                if (thumbnail != null)
                {
                    _thumbnailCache[file.Id] = thumbnail;
                }
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Error regenerating thumbnail for {FileId}", file.Id);
            }
        }

        _logger.Information("Regenerated {Count} thumbnails", _mediaFiles.Count);
    }

    private async Task<byte[]?> GenerateThumbnailAsync(MediaFile file)
    {
        return await Task.Run(() =>
        {
            try
            {
                if (file.Type == MediaType.Photo)
                {
                    return GenerateImageThumbnail(file.FullPath);
                }
                else
                {
                    return GenerateVideoThumbnail(file.FullPath);
                }
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Error generating thumbnail: {Path}", file.FullPath);
                return null;
            }
        });
    }

    private byte[]? GenerateImageThumbnail(string path)
    {
        using var image = System.Drawing.Image.FromFile(path);
        var thumbWidth = 160;
        var thumbHeight = (int)(image.Height * (160.0 / image.Width));

        using var thumbnail = image.GetThumbnailImage(thumbWidth, thumbHeight, () => false, IntPtr.Zero);
        using var ms = new MemoryStream();
        thumbnail.Save(ms, System.Drawing.Imaging.ImageFormat.Jpeg);
        return ms.ToArray();
    }

    private byte[]? GenerateVideoThumbnail(string path)
    {
        var firstFramePath = Path.Combine(_thumbnailCachePath, $"{Path.GetFileNameWithoutExtension(path)}.jpg");
        if (File.Exists(firstFramePath))
        {
            return File.ReadAllBytes(firstFramePath);
        }

        return null;
    }

    private void EnsureDirectoryExists(string path)
    {
        if (!Directory.Exists(path))
        {
            Directory.CreateDirectory(path);
        }
    }
}
