namespace Waller.Native.Core.Models;

public sealed record RenderedWallpaper(
    MonitorIdentity Monitor,
    string Path,
    int Width,
    int Height,
    DateTimeOffset CreatedAt);

public sealed record RenderRequest(
    MonitorSnapshot Monitor,
    PresetAssignment Assignment);
