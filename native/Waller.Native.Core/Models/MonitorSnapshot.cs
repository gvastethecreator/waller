namespace Waller.Native.Core.Models;

public sealed record MonitorSnapshot
{
    public MonitorSnapshot(
        MonitorIdentity identity,
        string displayName,
        WallpaperSource currentSource,
        WallpaperPlacement? currentPlacement = null)
    {
        ArgumentNullException.ThrowIfNull(identity);
        ArgumentNullException.ThrowIfNull(currentSource);

        Identity = identity;
        DisplayName = MonitorDisplayName.Normalize(displayName, nameof(displayName));
        CurrentSource = currentSource;
        CurrentPlacement = currentPlacement;
    }

    public MonitorIdentity Identity { get; }

    public string DisplayName { get; }

    public WallpaperSource CurrentSource { get; }

    public WallpaperPlacement? CurrentPlacement { get; }

    public MonitorBounds Bounds => Identity.Bounds;
}
