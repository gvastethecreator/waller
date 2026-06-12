namespace Waller.Native.Core.Models;

public enum WallpaperFitMode
{
    Cover,
    Contain,
    Stretch,
    Center,
    Tile,
}

public enum WallpaperAnchor
{
    TopLeft,
    Top,
    TopRight,
    Left,
    Center,
    Right,
    BottomLeft,
    Bottom,
    BottomRight,
}

public sealed record WallpaperPlacement
{
    private WallpaperFitMode fitMode;
    private WallpaperAnchor anchor;

    public WallpaperPlacement(
        WallpaperFitMode FitMode,
        WallpaperAnchor Anchor,
        int OffsetXPercent = 0,
        int OffsetYPercent = 0)
    {
        this.FitMode = FitMode;
        this.Anchor = Anchor;
        this.OffsetXPercent = OffsetXPercent;
        this.OffsetYPercent = OffsetYPercent;
    }

    public static WallpaperPlacement Default { get; } = new(WallpaperFitMode.Cover, WallpaperAnchor.Center);

    public WallpaperFitMode FitMode
    {
        get => fitMode;
        init
        {
            if (!Enum.IsDefined(value))
            {
                throw new ArgumentOutOfRangeException(nameof(value), value, "Wallpaper fit mode is invalid.");
            }

            fitMode = value;
        }
    }

    public WallpaperAnchor Anchor
    {
        get => anchor;
        init
        {
            if (!Enum.IsDefined(value))
            {
                throw new ArgumentOutOfRangeException(nameof(value), value, "Wallpaper anchor is invalid.");
            }

            anchor = value;
        }
    }

    public int OffsetXPercent { get; init; }

    public int OffsetYPercent { get; init; }

    public WallpaperPlacement NormalizeOffsets() => this with
    {
        OffsetXPercent = ClampOffset(OffsetXPercent),
        OffsetYPercent = ClampOffset(OffsetYPercent),
    };

    public static int ClampOffset(int offsetPercent) => Math.Clamp(offsetPercent, -100, 100);
}
