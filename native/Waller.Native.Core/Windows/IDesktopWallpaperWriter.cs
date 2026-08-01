namespace Waller.Native.Core.Windows;

internal interface IDesktopWallpaperWriter
{
    void SetWallpaper(
        string monitorId,
        string wallpaperPath,
        DesktopWallpaperPosition position);
}
