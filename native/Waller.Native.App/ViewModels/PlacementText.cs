using Waller.Native.Core.Models;

namespace Waller.Native.App.ViewModels;

internal static class PlacementText
{
    public static string Summary(WallpaperPlacement placement, LocalizedText text)
    {
        ArgumentNullException.ThrowIfNull(placement);
        ArgumentNullException.ThrowIfNull(text);

        var summary = $"{FitMode(placement.FitMode, text)} - {AnchorLabel(placement.Anchor, text)}";
        return placement.OffsetXPercent == 0 && placement.OffsetYPercent == 0
            ? summary
            : $"{summary} - {OffsetSummary(placement, text)}";
    }

    public static string FitMode(WallpaperFitMode fit, LocalizedText text)
    {
        ArgumentNullException.ThrowIfNull(text);

        return DefinedEnumValue.Require(
            fit,
            nameof(fit),
            PlacementTextErrors.UnknownFitMode) switch
        {
            WallpaperFitMode.Cover => text.FitCover,
            WallpaperFitMode.Contain => text.FitContain,
            WallpaperFitMode.Stretch => text.FitStretch,
            WallpaperFitMode.Center => text.FitCenter,
            WallpaperFitMode.Tile => text.FitTile,
            _ => InvalidFitMode(fit),
        };
    }

    public static string AnchorLabel(WallpaperAnchor anchor, LocalizedText text)
    {
        ArgumentNullException.ThrowIfNull(text);

        return DefinedEnumValue.Require(
            anchor,
            nameof(anchor),
            PlacementTextErrors.UnknownAnchor) switch
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
            _ => InvalidAnchor(anchor),
        };
    }

    private static string OffsetSummary(WallpaperPlacement placement, LocalizedText text) =>
        text.Format(text.OffsetSummaryFormat, placement.OffsetXPercent, placement.OffsetYPercent);

    private static string InvalidFitMode(WallpaperFitMode fit) =>
        throw new ArgumentOutOfRangeException(
            nameof(fit),
            fit,
            PlacementTextErrors.UnknownFitMode);

    private static string InvalidAnchor(WallpaperAnchor anchor) =>
        throw new ArgumentOutOfRangeException(
            nameof(anchor),
            anchor,
            PlacementTextErrors.UnknownAnchor);
}
