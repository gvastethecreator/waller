using Waller.Native.Core.Models;

namespace Waller.Native.Core.Windows;

internal sealed record DesktopWallpaperSnapshot
{
    private string monitorId = string.Empty;
    private MonitorBounds bounds = null!;

    public DesktopWallpaperSnapshot(
        string MonitorId,
        MonitorBounds Bounds,
        string? WallpaperPath)
    {
        if (string.IsNullOrWhiteSpace(MonitorId))
        {
            throw new ArgumentException("Monitor id is required.", nameof(MonitorId));
        }

        ArgumentNullException.ThrowIfNull(Bounds);

        monitorId = MonitorId;
        bounds = Bounds;
        this.WallpaperPath = WallpaperPath;
    }

    public string MonitorId
    {
        get => monitorId;
        init
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException("Monitor id is required.", nameof(value));
            }

            monitorId = value;
        }
    }

    public MonitorBounds Bounds
    {
        get => bounds;
        init
        {
            ArgumentNullException.ThrowIfNull(value);
            bounds = value;
        }
    }

    public string? WallpaperPath { get; init; }
}
