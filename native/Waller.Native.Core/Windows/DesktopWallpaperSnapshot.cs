using Waller.Native.Core.Models;

namespace Waller.Native.Core.Windows;

internal sealed record DesktopWallpaperSnapshot(
    string MonitorId,
    MonitorBounds Bounds,
    string? WallpaperPath);
