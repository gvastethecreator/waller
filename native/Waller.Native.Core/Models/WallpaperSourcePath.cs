namespace Waller.Native.Core.Models;

public static class WallpaperSourcePath
{
    public static string NormalizeImagePath(string imagePath)
    {
        if (!TryNormalizeImagePath(imagePath, out var normalized, out var error))
        {
            throw error ?? new WallpaperSourcePathException(
                WallpaperSourcePathException.Required,
                "Image source path is required.");
        }

        return normalized;
    }

    public static bool TryNormalizeImagePath(string? imagePath, out string normalized) =>
        TryNormalizeImagePath(imagePath, out normalized, out _);

    public static bool TryNormalizeImagePath(
        string? imagePath,
        out string normalized,
        out WallpaperSourcePathException? error)
    {
        normalized = string.Empty;
        error = null;
        if (string.IsNullOrWhiteSpace(imagePath))
        {
            error = new WallpaperSourcePathException(
                WallpaperSourcePathException.Required,
                "Image source path is required.");
            return false;
        }

        var trimmed = imagePath.Trim();
        if (!Path.IsPathFullyQualified(trimmed))
        {
            error = new WallpaperSourcePathException(
                WallpaperSourcePathException.FullyQualifiedRequired,
                "Image source path must be fully qualified.");
            return false;
        }

        if (!WallpaperImageFileTypes.IsSupportedImagePath(trimmed))
        {
            error = new WallpaperSourcePathException(
                WallpaperSourcePathException.UnsupportedFileType,
                "Image source file type is not supported.");
            return false;
        }

        normalized = trimmed;
        return true;
    }

    public static bool IsExistingImagePath(string? imagePath)
    {
        return TryNormalizeImagePath(imagePath, out var normalized)
            && File.Exists(normalized);
    }

    public static bool IsMissingImagePath(string? imagePath)
    {
        return TryNormalizeImagePath(imagePath, out var normalized)
            && !File.Exists(normalized);
    }

    public static string? FileName(string? imagePath)
    {
        return TryNormalizeImagePath(imagePath, out var normalized)
            ? Path.GetFileName(normalized)
            : null;
    }
}
