using Waller.Native.Core.Models;

namespace Waller.Native.Core.Windows;

public sealed class DesktopWallpaperApplier : IWallpaperApplier
{
    private const DesktopWallpaperPosition RenderedWallpaperPosition = DesktopWallpaperPosition.Fill;

    private readonly IDesktopWallpaperWriter writer;

    public DesktopWallpaperApplier()
        : this(new DesktopWallpaperComWriter())
    {
    }

    internal DesktopWallpaperApplier(IDesktopWallpaperWriter writer)
    {
        this.writer = writer;
    }

    public Task<ApplyResult> ApplyAsync(RenderedWallpaper wallpaper, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (!File.Exists(wallpaper.Path))
        {
            return Task.FromResult(ApplyResult.Failure(
                wallpaper.Monitor,
                ApplyErrorCodes.RenderedWallpaperMissing,
                $"Rendered wallpaper does not exist: {wallpaper.Path}"));
        }

        try
        {
            writer.SetWallpaper(wallpaper.Monitor.MonitorKey, wallpaper.Path, RenderedWallpaperPosition);

            return Task.FromResult(ApplyResult.Success(wallpaper.Monitor));
        }
        catch (Exception error)
        {
            return Task.FromResult(ApplyResult.Failure(
                wallpaper.Monitor,
                ApplyErrorCodes.WallpaperApplyFailed,
                error.Message));
        }
    }
}
