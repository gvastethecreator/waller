using Waller.Native.Core.Models;

namespace Waller.Native.Core.Windows;

internal sealed class DesktopWallpaperComReader : IDesktopWallpaperReader
{
    private readonly IDesktopWallpaper desktopWallpaper;

    public DesktopWallpaperComReader()
    {
        desktopWallpaper = DesktopWallpaperInterop.CreateDesktopWallpaper();
    }

    public WallpaperPlacement CurrentPlacement =>
        DesktopWallpaperInterop.GetPositionPlacementOrDefault(desktopWallpaper);

    public WallpaperSource BackgroundSource =>
        DesktopWallpaperInterop.GetBackgroundColorSourceOrEmpty(desktopWallpaper);

    public IReadOnlyList<DesktopWallpaperSnapshot> ReadMonitors(CancellationToken cancellationToken)
    {
        var count = DesktopWallpaperInterop.GetMonitorDevicePathCount(desktopWallpaper);
        var monitors = new List<DesktopWallpaperSnapshot>((int)count);

        for (uint index = 0; index < count; index++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var monitorId = DesktopWallpaperInterop.GetMonitorDevicePathAt(desktopWallpaper, index);
            monitors.Add(new DesktopWallpaperSnapshot(
                monitorId,
                DesktopWallpaperInterop.GetMonitorBounds(desktopWallpaper, monitorId),
                DesktopWallpaperInterop.GetWallpaper(desktopWallpaper, monitorId)));
        }

        return monitors;
    }
}
