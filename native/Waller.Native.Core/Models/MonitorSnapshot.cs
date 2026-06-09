namespace Waller.Native.Core.Models;

public sealed record MonitorSnapshot(
    MonitorIdentity Identity,
    string DisplayName,
    WallpaperSource CurrentSource,
    WallpaperPlacement? CurrentPlacement = null)
{
    public MonitorBounds Bounds => Identity.Bounds;
}
