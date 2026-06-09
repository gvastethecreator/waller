using Waller.Native.Core.Models;

namespace Waller.Native.Core.Windows;

public sealed class SampleMonitorDetector : IMonitorDetector
{
    public Task<IReadOnlyList<MonitorSnapshot>> DetectAsync(CancellationToken cancellationToken = default)
    {
        IReadOnlyList<MonitorSnapshot> monitors =
        [
            Create("DISPLAY-1", "Monitor 1", 1, 0, 0, 2560, 1440, WallpaperSource.FromImage(@"C:\Wallpapers\current-1.jpg")),
            Create("DISPLAY-2", "Monitor 2", 2, 2560, 120, 1920, 1080, WallpaperSource.Empty),
            Create("DISPLAY-3", "Monitor 3", 3, -1280, 220, 1280, 1024, WallpaperSource.FromSolidColor("#1f6feb")),
        ];

        return Task.FromResult(monitors);
    }

    private static MonitorSnapshot Create(
        string key,
        string name,
        int index,
        int x,
        int y,
        int width,
        int height,
        WallpaperSource source)
    {
        var identity = new MonitorIdentity(key, name, index, width, height, x, y);
        return new MonitorSnapshot(identity, name, source);
    }
}
