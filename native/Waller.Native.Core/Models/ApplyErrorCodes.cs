namespace Waller.Native.Core.Models;

public static class ApplyErrorCodes
{
    public const string MissingImageSource = "missing-image-source";
    public const string RenderedWallpaperMissing = "rendered-wallpaper-missing";
    public const string WallpaperApplyFailed = "wallpaper-apply-failed";

    public static bool IsKnown(string? errorCode) =>
        errorCode is MissingImageSource or RenderedWallpaperMissing or WallpaperApplyFailed;

    public static string Normalize(string? errorCode) =>
        IsKnown(errorCode)
            ? errorCode!
            : WallpaperApplyFailed;
}
