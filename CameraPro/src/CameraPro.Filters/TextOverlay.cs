using OpenCvSharp;
using System.Text;
using System.Globalization;

namespace CameraPro.Filters;

public class TextOverlay
{
    public string Text { get; set; } = "";
    public string Font { get; set; } = "Arial";
    public double FontSize { get; set; } = 24.0;
    public Scalar TextColor { get; set; } = new Scalar(255, 255, 255);
    public Scalar BackgroundColor { get; set; } = new Scalar(0, 0, 0, 0);
    public TextPosition Position { get; set; } = TextPosition.Top;
    public int OffsetX { get; set; } = 10;
    public int OffsetY { get; set; } = 10;
    public bool Shadow { get; set; } = true;
    public int ShadowOffset { get; set; } = 2;
    public bool IsEnabled { get; set; } = true;

    public Mat Apply(Mat input)
    {
        if (!IsEnabled || string.IsNullOrEmpty(Text) || input.Empty())
            return input;

        var result = input.Clone();
        var expandedText = ExpandText(result);

        foreach (var line in expandedText)
        {
            DrawTextLine(result, line.Text, line.Position);
        }

        return result;
    }

    private List<ExpandedTextLine> ExpandText(Mat image)
    {
        var lines = new List<ExpandedTextLine>();
        var size = GetTextSize(Text);
        var imageWidth = image.Width;
        var imageHeight = image.Height;

        var (x, y) = GetPosition(size.width, size.height, imageWidth, imageHeight);

        var textLines = Text.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
        var lineHeight = (int)(FontSize * 1.5);

        foreach (var line in textLines)
        {
            lines.Add(new ExpandedTextLine { Text = line, Position = new Point(x, y) });
            y += lineHeight;
        }

        return lines;
    }

    private (int x, int y) GetPosition(int textWidth, int textHeight, int imageWidth, int imageHeight)
    {
        int x = OffsetX;
        int y = OffsetY;

        switch (Position)
        {
            case TextPosition.Top:
                x = (imageWidth - textWidth) / 2;
                break;
            case TextPosition.Bottom:
                x = (imageWidth - textWidth) / 2;
                y = imageHeight - textHeight - OffsetY;
                break;
            case TextPosition.TopLeft:
                x = OffsetX;
                y = OffsetY;
                break;
            case TextPosition.TopRight:
                x = imageWidth - textWidth - OffsetX;
                y = OffsetY;
                break;
            case TextPosition.BottomLeft:
                x = OffsetX;
                y = imageHeight - textHeight - OffsetY;
                break;
            case TextPosition.BottomRight:
                x = imageWidth - textWidth - OffsetX;
                y = imageHeight - textHeight - OffsetY;
                break;
            case TextPosition.Center:
                x = (imageWidth - textWidth) / 2;
                y = (imageHeight - textHeight) / 2;
                break;
        }

        return (Math.Max(0, x), Math.Max(0, y));
    }

    private (int width, int height) GetTextSize(string text)
    {
        var width = (int)(text.Length * FontSize * 0.6);
        var height = (int)(FontSize * 1.5);
        return (width, height);
    }

    private void DrawTextLine(Mat image, string text, Point position)
    {
        if (string.IsNullOrEmpty(text))
            return;

        if (BackgroundColor.Alpha > 0)
        {
            var textSize = GetTextSize(text);
            var bgRect = new Rect(position.X - 5, position.Y - (int)FontSize, textSize.width + 10, textSize.height + 5);
            Cv2.Rectangle(image, bgRect, BackgroundColor, -1);
        }

        if (Shadow)
        {
            var shadowPos = new Point(position.X + ShadowOffset, position.Y + ShadowOffset);
            Cv2.PutText(image, text, shadowPos, HersheyFonts.HersheySimplex, FontSize / 30.0, new Scalar(0, 0, 0), (int)(FontSize / 12.0), LineTypes.AntiAlias);
        }

        Cv2.PutText(image, text, position, HersheyFonts.HersheySimplex, FontSize / 30.0, TextColor, (int)(FontSize / 12.0), LineTypes.AntiAlias);
    }
}

public class TimestampOverlay : TextOverlay
{
    public bool ShowDate { get; set; } = true;
    public bool ShowTime { get; set; } = true;
    public string DateFormat { get; set; } = "yyyy-MM-dd";
    public string TimeFormat { get; set; } = "HH:mm:ss";

    public void UpdateTimestamp()
    {
        if (!ShowDate && !ShowTime)
        {
            Text = "";
            return;
        }

        var sb = new StringBuilder();
        if (ShowDate)
        {
            sb.Append(DateTime.Now.ToString(DateFormat));
        }
        if (ShowDate && ShowTime)
        {
            sb.Append(" ");
        }
        if (ShowTime)
        {
            sb.Append(DateTime.Now.ToString(TimeFormat));
        }

        Text = sb.ToString();
    }
}

public class LogoOverlay
{
    public string? LogoPath { get; set; }
    public Mat? LogoImage { get; set; }
    public TextPosition Position { get; set; } = TextPosition.BottomRight;
    public int OffsetX { get; set; } = 10;
    public int OffsetY { get; set; } = 10;
    public double Scale { get; set; } = 0.15;
    public bool IsEnabled { get; set; } = true;
    public int Opacity { get; set; } = 100;

    public Mat Apply(Mat input)
    {
        if (!IsEnabled || input.Empty())
            return input;

        if (LogoImage == null && !string.IsNullOrEmpty(LogoPath))
        {
            try
            {
                LogoImage = Cv2.ImRead(LogoPath, ImreadModes.Unchanged);
            }
            catch
            {
                return input;
            }
        }

        if (LogoImage == null || LogoImage.Empty())
            return input;

        var result = input.Clone();
        var logoWidth = (int)(input.Width * Scale);
        var logoHeight = (int)(LogoImage.Height * ((double)logoWidth / LogoImage.Width));

        var resizedLogo = new Mat();
        Cv2.Resize(LogoImage, resizedLogo, new OpenCvSharp.Size(logoWidth, logoHeight));

        if (resizedLogo.Channels() == 4)
        {
            var (x, y) = GetPosition(logoWidth, logoHeight, input.Width, input.Height);

            var roi = new Mat(result, new Rect(x, y, logoWidth, logoHeight));
            var alpha = new Mat();
            resizedLogo.Split()[3].CopyTo(alpha);

            if (Opacity < 100)
            {
                alpha.ConvertTo(alpha, MatType.CV_32F, Opacity / 100.0);
            }

            var rgb = new Mat();
            Cv2.ExtractChannel(resizedLogo, rgb, 0);
            Cv2.ExtractChannel(resizedLogo, rgb, 1);
            Cv2.ExtractChannel(resizedLogo, rgb, 2);

            Cv2.Add(roi, rgb, roi);

            alpha.Dispose();
            rgb.Dispose();
            resizedLogo.Dispose();
        }
        else
        {
            var (x, y) = GetPosition(logoWidth, logoHeight, input.Width, input.Height);
            var roi = new Mat(result, new Rect(x, y, logoWidth, logoHeight));
            resizedLogo.CopyTo(roi);
            resizedLogo.Dispose();
        }

        return result;
    }

    private (int x, int y) GetPosition(int logoWidth, int logoHeight, int imageWidth, int imageHeight)
    {
        int x = OffsetX;
        int y = OffsetY;

        switch (Position)
        {
            case TextPosition.TopLeft:
                break;
            case TextPosition.TopRight:
                x = imageWidth - logoWidth - OffsetX;
                break;
            case TextPosition.BottomLeft:
                y = imageHeight - logoHeight - OffsetY;
                break;
            case TextPosition.BottomRight:
                x = imageWidth - logoWidth - OffsetX;
                y = imageHeight - logoHeight - OffsetY;
                break;
            case TextPosition.Center:
                x = (imageWidth - logoWidth) / 2;
                y = (imageHeight - logoHeight) / 2;
                break;
        }

        return (Math.Max(0, x), Math.Max(0, y));
    }
}

public class ExpandedTextLine
{
    public string Text { get; set; } = "";
    public Point Position { get; set; }
}

public enum TextPosition
{
    Top,
    Bottom,
    TopLeft,
    TopRight,
    BottomLeft,
    BottomRight,
    Center
}