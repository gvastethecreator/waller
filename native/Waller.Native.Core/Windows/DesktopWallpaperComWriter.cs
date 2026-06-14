namespace Waller.Native.Core.Windows;

internal sealed class DesktopWallpaperComWriter : IDesktopWallpaperWriter
{
    public void SetWallpaper(
        string monitorId,
        string wallpaperPath,
        DesktopWallpaperPosition position)
    {
        StaThreadRunner.Run(() =>
        {
            var desktopWallpaper = DesktopWallpaperInterop.CreateDesktopWallpaper();
            DesktopWallpaperInterop.SetWallpaperThenPosition(desktopWallpaper, monitorId, wallpaperPath, position);
        });
    }
}
