using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace CameraPro.MultiCamera;

public static class GridLayout
{
    public static BitmapSource Render2x2(Dictionary<string, BitmapSource> frames)
    {
        int width = 1920;
        int height = 1080;
        int cellWidth = width / 2;
        int cellHeight = height / 2;
        
        var drawingVisual = new DrawingVisual();
        using (var context = drawingVisual.RenderOpen())
        {
            int index = 0;
            foreach (var frame in frames.Values.Take(4))
            {
                int row = index / 2;
                int col = index % 2;
                
                context.DrawImage(frame, new Rect(col * cellWidth, row * cellHeight, cellWidth, cellHeight));
                index++;
            }
        }
        
        var renderBitmap = new RenderTargetBitmap(width, height, 96, 96, PixelFormats.Pbgra32);
        renderBitmap.Render(drawingVisual);
        renderBitmap.Freeze();
        return renderBitmap;
    }
}
