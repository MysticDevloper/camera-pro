using CameraPro.Core.Interfaces;
using Serilog;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;

namespace CameraPro.Storage;

public class ImageExporter : IImageExporter
{
    private readonly ILogger _logger;
    private readonly IFileManager _fileManager;

    public ImageExporter(IFileManager fileManager)
    {
        _fileManager = fileManager;
        _logger = Log.ForContext<ImageExporter>();
    }

    public async Task<string> ExportAsync(string inputPath, string outputPath, ImageFormat format, int quality = 90)
    {
        if (!File.Exists(inputPath))
        {
            throw new FileNotFoundException("Input file not found", inputPath);
        }

        return await Task.Run(() =>
        {
            var encoder = GetEncoder(format);
            var encoderParams = new EncoderParameters(1);
            encoderParams.Param[0] = new EncoderParameter(Encoder.Quality, quality);

            using var image = Image.FromFile(inputPath);
            image.Save(outputPath, encoder, encoderParams);

            _logger.Information("Exported {Input} to {Output} as {Format}", inputPath, outputPath, format);
            return outputPath;
        });
    }

    public async Task<List<string>> BatchResizeAsync(List<string> inputPaths, string outputFolder, int maxWidth, int maxHeight)
    {
        var outputFiles = new List<string>();

        if (!Directory.Exists(outputFolder))
        {
            Directory.CreateDirectory(outputFolder);
        }

        foreach (var inputPath in inputPaths)
        {
            try
            {
                var outputPath = await ResizeImageAsync(inputPath, outputFolder, maxWidth, maxHeight);
                if (!string.IsNullOrEmpty(outputPath))
                {
                    outputFiles.Add(outputPath);
                }
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Error resizing image: {Path}", inputPath);
            }
        }

        _logger.Information("Batch resized {Count} images to {Folder}", outputFiles.Count, outputFolder);
        return outputFiles;
    }

    public async Task<List<string>> BatchConvertAsync(List<string> inputPaths, string outputFolder, ImageFormat format)
    {
        var outputFiles = new List<string>();

        if (!Directory.Exists(outputFolder))
        {
            Directory.CreateDirectory(outputFolder);
        }

        var extension = format switch
        {
            ImageFormat.JPEG => ".jpg",
            ImageFormat.PNG => ".png",
            ImageFormat.BMP => ".bmp",
            ImageFormat.TIFF => ".tiff",
            _ => ".jpg"
        };

        foreach (var inputPath in inputPaths)
        {
            try
            {
                var fileName = Path.GetFileNameWithoutExtension(inputPath) + extension;
                var outputPath = Path.Combine(outputFolder, fileName);
                await ExportAsync(inputPath, outputPath, format);
                outputFiles.Add(outputPath);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Error converting image: {Path}", inputPath);
            }
        }

        _logger.Information("Batch converted {Count} images to {Format}", outputFiles.Count, format);
        return outputFiles;
    }

    public async Task<string> ApplyWatermarkAsync(string inputPath, string outputPath, string watermarkPath, WatermarkPosition position)
    {
        if (!File.Exists(inputPath))
        {
            throw new FileNotFoundException("Input file not found", inputPath);
        }

        if (!File.Exists(watermarkPath))
        {
            throw new FileNotFoundException("Watermark file not found", watermarkPath);
        }

        return await Task.Run(() =>
        {
            using var image = Image.FromFile(inputPath);
            using var watermark = Image.FromFile(watermarkPath);

            var watermarkWidth = image.Width / 5;
            var watermarkHeight = (int)(watermark.Height * (watermarkWidth / (double)watermark.Width));

            using var resizedWatermark = ResizeImage(watermark, watermarkWidth, watermarkHeight);

            using var graphics = Graphics.FromImage(image);
            graphics.SmoothingMode = SmoothingMode.AntiAlias;
            graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;

            var x = position switch
            {
                WatermarkPosition.TopLeft => 10,
                WatermarkPosition.TopRight => image.Width - watermarkWidth - 10,
                WatermarkPosition.BottomLeft => 10,
                WatermarkPosition.BottomRight => image.Width - watermarkWidth - 10,
                WatermarkPosition.Center => (image.Width - watermarkWidth) / 2,
                _ => 10
            };

            var y = position switch
            {
                WatermarkPosition.TopLeft => 10,
                WatermarkPosition.TopRight => 10,
                WatermarkPosition.BottomLeft => image.Height - watermarkHeight - 10,
                WatermarkPosition.BottomRight => image.Height - watermarkHeight - 10,
                WatermarkPosition.Center => (image.Height - watermarkHeight) / 2,
                _ => image.Height - watermarkHeight - 10
            };

            graphics.DrawImage(resizedWatermark, x, y, watermarkWidth, watermarkHeight);
            image.Save(outputPath, ImageFormat.Jpeg);

            _logger.Information("Applied watermark to {Input}", inputPath);
            return outputPath;
        });
    }

    public async Task BatchRenameAsync(List<string> fileIds, string template)
    {
        var counter = 1;
        foreach (var fileId in fileIds)
        {
            try
            {
                var fileInfo = await _fileManager.GetFileInfoAsync(fileId);
                if (string.IsNullOrEmpty(fileInfo.FullPath) || !File.Exists(fileInfo.FullPath))
                {
                    continue;
                }

                var newName = template
                    .Replace("{counter}", counter.ToString("D3"))
                    .Replace("{date}", DateTime.Now.ToString("yyyy-MM-dd"))
                    .Replace("{time}", DateTime.Now.ToString("HH-mm-ss"));

                var extension = Path.GetExtension(fileInfo.FullPath);
                var newFileName = newName + extension;
                var directory = Path.GetDirectoryName(fileInfo.FullPath) ?? string.Empty;
                var newPath = Path.Combine(directory, newFileName);

                if (File.Exists(newPath))
                {
                    newPath = Path.Combine(directory, $"{newName}_{counter}{extension}");
                }

                File.Move(fileInfo.FullPath, newPath);
                counter++;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Error renaming file: {FileId}", fileId);
            }
        }

        _logger.Information("Batch renamed {Count} files with template: {Template}", counter - 1, template);
    }

    private async Task<string?> ResizeImageAsync(string inputPath, string outputFolder, int maxWidth, int maxHeight)
    {
        return await Task.Run(() =>
        {
            using var image = Image.FromFile(inputPath);

            var ratioX = maxWidth / (double)image.Width;
            var ratioY = maxHeight / (double)image.Height;
            var ratio = Math.Min(ratioX, ratioY);

            var newWidth = (int)(image.Width * ratio);
            var newHeight = (int)(image.Height * ratio);

            using var resizedImage = ResizeImage(image, newWidth, newHeight);

            var fileName = Path.GetFileNameWithoutExtension(inputPath) + "_resized.jpg";
            var outputPath = Path.Combine(outputFolder, fileName);

            var encoder = GetEncoder(ImageFormat.JPEG);
            var encoderParams = new EncoderParameters(1);
            encoderParams.Param[0] = new EncoderParameter(Encoder.Quality, 85L);
            resizedImage.Save(outputPath, encoder, encoderParams);

            return outputPath;
        });
    }

    private Image ResizeImage(Image image, int width, int height)
    {
        var destRect = new Rectangle(0, 0, width, height);
        var destImage = new Bitmap(width, height);

        destImage.SetResolution(image.HorizontalResolution, image.VerticalResolution);

        using var graphics = Graphics.FromImage(destImage);
        graphics.CompositingMode = CompositingMode.SourceCopy;
        graphics.CompositingQuality = CompositingQuality.HighQuality;
        graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
        graphics.SmoothingMode = SmoothingMode.HighQuality;
        graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;

        using var wrapMode = new ImageAttributes();
        wrapMode.SetWrapMode(WrapMode.TileFlipXY);
        graphics.DrawImage(image, destRect, 0, 0, image.Width, image.Height, GraphicsUnit.Pixel, wrapMode);

        return destImage;
    }

    private ImageCodecInfo GetEncoder(ImageFormat format)
    {
        var codecs = ImageCodecInfo.GetImageDecoders();
        foreach (var codec in codecs)
        {
            if (codec.FormatID == format.Guid)
            {
                return codec;
            }
        }
        return codecs[0];
    }
}
