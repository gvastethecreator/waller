using Waller.Native.Core.Models;

namespace Waller.Native.Core.Windows;

internal interface IDesktopWallpaperReader
{
    WallpaperPlacement CurrentPlacement { get; }

    WallpaperSource BackgroundSource { get; }

    IReadOnlyList<DesktopWallpaperSnapshot> ReadMonitors(CancellationToken cancellationToken);
}
