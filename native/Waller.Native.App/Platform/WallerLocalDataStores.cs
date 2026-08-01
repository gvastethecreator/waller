using Waller.Native.Core.Presets;
using Waller.Native.Core.Rendering;
using Waller.Native.Core.Settings;
using Waller.Native.Workflows.Storage;

namespace Waller.Native.App.Platform;

internal sealed record WallerLocalDataStores
{
    public WallerLocalDataStores(
        PresetStore Presets,
        UserSettingsStore Settings,
        RenderedWallpaperStore RenderedWallpapers)
    {
        ArgumentNullException.ThrowIfNull(Presets);
        ArgumentNullException.ThrowIfNull(Settings);
        ArgumentNullException.ThrowIfNull(RenderedWallpapers);

        this.Presets = Presets;
        this.Settings = Settings;
        this.RenderedWallpapers = RenderedWallpapers;
    }

    public PresetStore Presets { get; }

    public UserSettingsStore Settings { get; }

    public RenderedWallpaperStore RenderedWallpapers { get; }

    public static WallerLocalDataStores CreateDefault() =>
        Create(WallerAppDataPaths.Current);

    public static WallerLocalDataStores Create(string rootDirectory) =>
        Create(new LocalDataLayout(rootDirectory, rootDirectory));

    public static WallerLocalDataStores Create(LocalDataLayout layout)
    {
        ArgumentNullException.ThrowIfNull(layout);
        return new(
            new PresetStore(layout.AppDataRoot),
            new UserSettingsStore(layout.AppDataRoot),
            new RenderedWallpaperStore(layout.RenderedCacheRoot));
    }
}
