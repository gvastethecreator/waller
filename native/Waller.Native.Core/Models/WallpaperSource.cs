namespace Waller.Native.Core.Models;

public enum WallpaperSourceKind
{
    Image,
    SolidColor,
    Empty,
}

public sealed record WallpaperSource(
    WallpaperSourceKind Kind,
    string? ImagePath = null,
    string? ColorHex = null)
{
    public static WallpaperSource Empty { get; } = new(WallpaperSourceKind.Empty);

    public static WallpaperSource FromImage(string imagePath)
    {
        return new WallpaperSource(WallpaperSourceKind.Image, WallpaperSourcePath.NormalizeImagePath(imagePath));
    }

    public static WallpaperSource FromSolidColor(string colorHex)
    {
        var normalized = ColorHexValue.Normalize(colorHex);
        return new WallpaperSource(WallpaperSourceKind.SolidColor, ColorHex: normalized);
    }

    public static WallpaperSource? TryNormalize(WallpaperSource? source)
    {
        if (source is null || !Enum.IsDefined(source.Kind))
        {
            return null;
        }

        return source.Kind switch
        {
            WallpaperSourceKind.Empty => Empty,
            WallpaperSourceKind.Image => WallpaperSourcePath.TryNormalizeImagePath(source.ImagePath, out var imagePath)
                ? new WallpaperSource(WallpaperSourceKind.Image, imagePath)
                : null,
            WallpaperSourceKind.SolidColor => ColorHexValue.TryParse(source.ColorHex, out var color)
                ? new WallpaperSource(WallpaperSourceKind.SolidColor, ColorHex: color.ToHex())
                : null,
            _ => null,
        };
    }

    public static string NormalizeColorHex(string colorHex) => ColorHexValue.Normalize(colorHex);
}
