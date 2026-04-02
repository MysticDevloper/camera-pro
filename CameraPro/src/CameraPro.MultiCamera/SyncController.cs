namespace CameraPro.MultiCamera;

public class SyncController
{
    private readonly object _lock = new();
    private Dictionary<string, DateTime> _lastFrameTimes = new();
    private const int MaxSyncDifferenceMs = 33; // ~30fps tolerance
    
    public bool AreFramesSynced(Dictionary<string, DateTime> frameTimes)
    {
        if (frameTimes.Count < 2) return true;
        
        var times = frameTimes.Values.ToList();
        var min = times.Min();
        var max = times.Max();
        
        return (max - min).TotalMilliseconds <= MaxSyncDifferenceMs;
    }
    
    public DateTime GetSyncReference(Dictionary<string, DateTime> frameTimes)
    {
        return frameTimes.Values.Min();
    }
}
