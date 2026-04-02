using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace CameraPro.MultiCamera;

public enum PiPPosition { TopLeft, TopRight, BottomLeft, BottomRight }

public static class PiPLayout
{
    public static BitmapSource Render(
        BitmapSource mainFrame,
        BitmapSource pipFrame,
        PiPPosition position,
        double pipSizePercent = 20)
    {
        int width = mainFrame.PixelWidth;
        int height = mainFrame.PixelHeight;
        int pipWidth = (int)(width * pipSizePercent / 100);
        int pipHeight = (int)(pipWidth * pipFrame.PixelHeight / pipFrame.PixelWidth);
        
        var drawingVisual = new DrawingVisual();
        using (var context = drawingVisual.RenderOpen())
        {
            // Draw main frame
            context.DrawImage(mainFrame, new Rect(0, 0, width, height));
            
            // Calculate PiP position
            double x = position switch
            {
                PiPPosition.TopLeft or PiPPosition.BottomLeft => 10,
                PiPPosition.TopRight or PiPPosition.BottomRight => width - pipWidth - 10,
                _ => 10
            };
            
            double y = position switch
            {
                PiPPosition.TopLeft or PiPPosition.TopRight => 10,
                PiPPosition.BottomLeft or PiPPosition.BottomRight => height - pipHeight - 10,
                _ => 10
            };
            
            // Draw PiP frame with border
            context.DrawRectangle(
                new SolidColorBrush(Colors.White),
                new Pen(Brushes.White, 2),
                new Rect(x - 1, y - 1, pipWidth + 2, pipHeight + 2));
            context.DrawImage(pipFrame, new Rect(x, y, pipWidth, pipHeight));
        }
        
        var renderBitmap = new RenderTargetBitmap(width, height, 96, 96, PixelFormats.Pbgra32);
        renderBitmap.Render(drawingVisual);
        renderBitmap.Freeze();
        return renderBitmap;
    }
}
