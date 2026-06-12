using Waller.Native.Core.Models;

namespace Waller.Native.App.ViewModels;

internal static class PlacementText
{
    public static string Summary(WallpaperPlacement placement, LocalizedText text)
    {
        var summary = $"{FitMode(placement.FitMode, text)} - {AnchorLabel(placement.Anchor, text)}";
        return placement.OffsetXPercent == 0 && placement.OffsetYPercent == 0
            ? summary
            : $"{summary} - {OffsetSummary(placement, text)}";
    }

    public static string FitMode(WallpaperFitMode fit, LocalizedText text) => fit switch
    {
        WallpaperFitMode.Cover => text.FitCover,
        WallpaperFitMode.Contain => text.FitContain,
        WallpaperFitMode.Stretch => text.FitStretch,
        WallpaperFitMode.Center => text.FitCenter,
        WallpaperFitMode.Tile => text.FitTile,
        _ => text.UnsupportedValue,
    };

    public static string AnchorLabel(WallpaperAnchor anchor, LocalizedText text) => anchor switch
    {
        WallpaperAnchor.TopLeft => text.AnchorTopLeft,
        WallpaperAnchor.Top => text.AnchorTop,
        WallpaperAnchor.TopRight => text.AnchorTopRight,
        WallpaperAnchor.Left => text.AnchorLeft,
        WallpaperAnchor.Center => text.AnchorCenter,
        WallpaperAnchor.Right => text.AnchorRight,
        WallpaperAnchor.BottomLeft => text.AnchorBottomLeft,
        WallpaperAnchor.Bottom => text.AnchorBottom,
        WallpaperAnchor.BottomRight => text.AnchorBottomRight,
        _ => text.UnsupportedValue,
    };

    private static string OffsetSummary(WallpaperPlacement placement, LocalizedText text) =>
        text.Format(text.OffsetSummaryFormat, placement.OffsetXPercent, placement.OffsetYPercent);
}
