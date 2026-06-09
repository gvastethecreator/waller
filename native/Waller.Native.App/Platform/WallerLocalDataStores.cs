using Waller.Native.Core.Presets;
using Waller.Native.Core.Rendering;
using Waller.Native.Core.Settings;

namespace Waller.Native.App.Platform;

internal sealed record WallerLocalDataStores(
    PresetStore Presets,
    UserSettingsStore Settings,
    RenderedWallpaperStore RenderedWallpapers)
{
    public static WallerLocalDataStores CreateDefault() =>
        Create(WallerAppDataPaths.Root);

    public static WallerLocalDataStores Create(string rootDirectory) =>
        new(
            new PresetStore(rootDirectory),
            new UserSettingsStore(rootDirectory),
            new RenderedWallpaperStore(rootDirectory));
}
