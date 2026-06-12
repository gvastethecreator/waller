using Waller.Native.Core.Rendering;
using Waller.Native.Core.Sessions;
using Waller.Native.Core.Windows;

namespace Waller.Native.App.Platform;

internal sealed record WallerAppServices
{
    public WallerAppServices(
        IMonitorDetector PrimaryMonitorDetector,
        IMonitorDetector FallbackMonitorDetector,
        IImageFilePicker ImageFilePicker,
        WallpaperApplyService ApplyService,
        WallerLocalDataStores LocalData)
    {
        ArgumentNullException.ThrowIfNull(PrimaryMonitorDetector);
        ArgumentNullException.ThrowIfNull(FallbackMonitorDetector);
        ArgumentNullException.ThrowIfNull(ImageFilePicker);
        ArgumentNullException.ThrowIfNull(ApplyService);
        ArgumentNullException.ThrowIfNull(LocalData);

        this.PrimaryMonitorDetector = PrimaryMonitorDetector;
        this.FallbackMonitorDetector = FallbackMonitorDetector;
        this.ImageFilePicker = ImageFilePicker;
        this.ApplyService = ApplyService;
        this.LocalData = LocalData;
    }

    public IMonitorDetector PrimaryMonitorDetector { get; }

    public IMonitorDetector FallbackMonitorDetector { get; }

    public IImageFilePicker ImageFilePicker { get; }

    public WallpaperApplyService ApplyService { get; }

    public WallerLocalDataStores LocalData { get; }

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
