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
        ArgumentNullException.ThrowIfNull(Bounds);

        monitorId = MonitorKeys.Require(MonitorId, nameof(MonitorId));
        bounds = Bounds;
        this.WallpaperPath = WallpaperPath;
    }

    public string MonitorId
    {
        get => monitorId;
        init
        {
            monitorId = MonitorKeys.Require(value, nameof(value));
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
