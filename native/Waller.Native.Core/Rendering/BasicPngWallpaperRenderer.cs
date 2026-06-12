using Waller.Native.Core.Models;

namespace Waller.Native.Core.Rendering;

public sealed class BasicPngWallpaperRenderer : IWallpaperRenderer
{
    private readonly RenderedWallpaperStore store;

    public BasicPngWallpaperRenderer(RenderedWallpaperStore store)
    {
        ArgumentNullException.ThrowIfNull(store);
        this.store = store;
    }

    public async Task<RenderedWallpaper> RenderAsync(
        RenderRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        cancellationToken.ThrowIfCancellationRequested();

        var monitor = request.Monitor;
        if (monitor.Bounds.Width <= 0 || monitor.Bounds.Height <= 0)
        {
            throw new InvalidOperationException(
                $"Monitor has invalid render bounds: {monitor.Bounds.Width}x{monitor.Bounds.Height}.");
        }

        var outputPath = store.CreatePath(monitor.Identity.MonitorKey);
        var pixels = request.Assignment.Source.Kind switch
        {
            WallpaperSourceKind.Empty => PixelBuffer.CreateSolid(
                monitor.Bounds.Width,
                monitor.Bounds.Height,
                RgbColor.Black),
            WallpaperSourceKind.SolidColor => PixelBuffer.CreateSolid(
                monitor.Bounds.Width,
                monitor.Bounds.Height,
                RgbColor.FromHex(request.Assignment.Source.ColorHex ?? "#000000")),
            WallpaperSourceKind.Image => ImagePlacementRenderer.Render(
                await ImageSourceDecoder.DecodeAsync(
                    request.Assignment.Source.ImagePath,
                    cancellationToken),
                monitor.Bounds.Width,
                monitor.Bounds.Height,
                request.Assignment.Placement),
            _ => PixelBuffer.CreateSolid(
                monitor.Bounds.Width,
                monitor.Bounds.Height,
                RgbColor.Black),
        };

        await SolidColorPngWriter.WriteAsync(
            outputPath,
            pixels,
            cancellationToken);

        return new RenderedWallpaper(
            monitor.Identity,
            outputPath,
            monitor.Bounds.Width,
            monitor.Bounds.Height,
            DateTimeOffset.UtcNow);
    }
}
