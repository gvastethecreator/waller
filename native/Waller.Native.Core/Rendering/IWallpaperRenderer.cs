using Waller.Native.Core.Models;

namespace Waller.Native.Core.Rendering;

public interface IWallpaperRenderer
{
    Task<RenderedWallpaper> RenderAsync(RenderRequest request, CancellationToken cancellationToken = default);
}
