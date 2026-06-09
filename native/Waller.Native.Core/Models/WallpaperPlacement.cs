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

public sealed record WallpaperPlacement(
    WallpaperFitMode FitMode,
    WallpaperAnchor Anchor,
    int OffsetXPercent = 0,
    int OffsetYPercent = 0)
{
    public static WallpaperPlacement Default { get; } = new(WallpaperFitMode.Cover, WallpaperAnchor.Center);

    public WallpaperPlacement NormalizeOffsets() => this with
    {
        OffsetXPercent = ClampOffset(OffsetXPercent),
        OffsetYPercent = ClampOffset(OffsetYPercent),
    };

    public static int ClampOffset(int offsetPercent) => Math.Clamp(offsetPercent, -100, 100);
}
