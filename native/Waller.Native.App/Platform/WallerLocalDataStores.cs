using Waller.Native.Core.Presets;
using Waller.Native.Core.Rendering;
using Waller.Native.Core.Settings;

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
        Create(WallerAppDataPaths.Root);

    public static WallerLocalDataStores Create(string rootDirectory) =>
        new(
            new PresetStore(rootDirectory),
            new UserSettingsStore(rootDirectory),
            new RenderedWallpaperStore(rootDirectory));
}
