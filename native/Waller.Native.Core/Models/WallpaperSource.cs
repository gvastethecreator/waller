namespace Waller.Native.Core.Models;

public enum WallpaperSourceKind
{
    Image,
    SolidColor,
    Empty,
}

public sealed record WallpaperSource
{
    private WallpaperSourceKind kind;

    public WallpaperSource(
        WallpaperSourceKind Kind,
        string? ImagePath = null,
        string? ColorHex = null)
    {
        this.Kind = Kind;
        this.ImagePath = ImagePath;
        this.ColorHex = ColorHex;
    }

    public WallpaperSourceKind Kind
    {
        get => kind;
        init
        {
            if (!Enum.IsDefined(value))
            {
                throw new ArgumentOutOfRangeException(nameof(value), value, "Wallpaper source kind is not supported.");
            }

            kind = value;
        }
    }

    public string? ImagePath { get; init; }

    public string? ColorHex { get; init; }

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
