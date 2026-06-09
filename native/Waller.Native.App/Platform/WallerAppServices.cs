using Waller.Native.Core.Rendering;
using Waller.Native.Core.Sessions;
using Waller.Native.Core.Windows;

namespace Waller.Native.App.Platform;

internal sealed record WallerAppServices(
    IMonitorDetector PrimaryMonitorDetector,
    IMonitorDetector FallbackMonitorDetector,
    IImageFilePicker ImageFilePicker,
    WallpaperApplyService ApplyService,
    WallerLocalDataStores LocalData)
{
    public static WallerAppServices CreateDefault()
    {
        var localData = WallerLocalDataStores.CreateDefault();
        var renderer = new BasicPngWallpaperRenderer(localData.RenderedWallpapers);

        return new WallerAppServices(
            new WindowsMonitorDetector(),
            new EmptyMonitorDetector(),
            new ImageFilePicker(),
            new WallpaperApplyService(renderer, new DesktopWallpaperApplier()),
            localData);
    }
}
