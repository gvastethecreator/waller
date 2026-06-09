using Waller.Native.Core.Models;

namespace Waller.Native.Core.Windows;

public interface IWallpaperApplier
{
    Task<ApplyResult> ApplyAsync(RenderedWallpaper wallpaper, CancellationToken cancellationToken = default);
}
