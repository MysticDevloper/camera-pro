namespace CameraPro.Core.Models;

public class FilterEffect
{
    public string Name { get; set; } = string.Empty;
    public bool IsEnabled { get; set; } = true;
    public Dictionary<string, double> Parameters { get; set; } = new();
}