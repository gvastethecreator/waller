using Waller.Native.Core.Rendering;

namespace Waller.Native.App.ViewModels;

internal static class RenderedCacheCleanup
{
    public static RenderedCacheClearResult Clear(RenderedWallpaperStore store) =>
        store.Clear();
}
