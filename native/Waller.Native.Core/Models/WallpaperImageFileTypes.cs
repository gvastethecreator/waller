namespace Waller.Native.Core.Models;

public static class WallpaperImageFileTypes
{
    public static IReadOnlyList<string> PickerExtensions { get; } =
    [
        ".jpg",
        ".jpeg",
        ".png",
        ".bmp",
        ".webp",
        ".gif",
        ".tif",
        ".tiff",
        ".heic",
        ".heif",
    ];

    public static bool IsSupportedImagePath(string imagePath) =>
        PickerExtensions.Contains(Path.GetExtension(imagePath), StringComparer.OrdinalIgnoreCase);
}
