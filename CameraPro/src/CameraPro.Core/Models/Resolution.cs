namespace CameraPro.Core.Models;

public class Resolution
{
    public int Width { get; set; }
    public int Height { get; set; }

    public override string ToString() => $"{Width}x{Height}";
}