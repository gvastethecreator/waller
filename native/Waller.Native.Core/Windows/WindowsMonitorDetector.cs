using Waller.Native.Core.Models;

namespace Waller.Native.Core.Windows;

public sealed class WindowsMonitorDetector : IMonitorDetector
{
    private readonly IDesktopWallpaperReader reader;

    public WindowsMonitorDetector()
        : this(new DesktopWallpaperComReader())
    {
    }

    internal WindowsMonitorDetector(IDesktopWallpaperReader reader)
    {
        this.reader = reader;
    }

    public Task<IReadOnlyList<MonitorSnapshot>> DetectAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var currentPlacement = reader.CurrentPlacement;
        var backgroundSource = reader.BackgroundSource;
        var desktopMonitors = reader.ReadMonitors(cancellationToken);
        var monitors = new List<MonitorSnapshot>(desktopMonitors.Count);

        for (var index = 0; index < desktopMonitors.Count; index++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var desktopMonitor = desktopMonitors[index];
            var source = DesktopWallpaperInterop.WallpaperPathToSource(desktopMonitor.WallpaperPath);
            if (source.Kind == WallpaperSourceKind.Empty)
            {
                source = backgroundSource;
            }
            var displayIndex = checked(index + 1);
            var deviceName = DesktopMonitorDisplayName.ShortenDeviceName(desktopMonitor.MonitorId);
            var displayName = DesktopMonitorDisplayName.Create(displayIndex, desktopMonitor.MonitorId);

            var identity = new MonitorIdentity(
                desktopMonitor.MonitorId,
                deviceName,
                displayIndex,
                desktopMonitor.Bounds.Width,
                desktopMonitor.Bounds.Height,
                desktopMonitor.Bounds.X,
                desktopMonitor.Bounds.Y);

            monitors.Add(new MonitorSnapshot(identity, displayName, source, currentPlacement));
        }

        return Task.FromResult<IReadOnlyList<MonitorSnapshot>>(monitors);
    }
}
