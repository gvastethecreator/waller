namespace Waller.Native.Core.Models;

public static class WallpaperSourceFiles
{
    public static bool IsMissingImageFile(WallpaperSource source)
    {
        ArgumentNullException.ThrowIfNull(source);

        return source.Kind == WallpaperSourceKind.Image
            && WallpaperSourcePath.IsMissingImagePath(source.ImagePath);
    }

    public static bool HasExistingImageFile(WallpaperSource source)
    {
        ArgumentNullException.ThrowIfNull(source);

        return source.Kind == WallpaperSourceKind.Image
            && WallpaperSourcePath.IsExistingImagePath(source.ImagePath);
    }

    public static string? ImageFileName(WallpaperSource source)
    {
        ArgumentNullException.ThrowIfNull(source);

        return source.Kind == WallpaperSourceKind.Image
            ? WallpaperSourcePath.FileName(source.ImagePath)
            : null;
    }
}
