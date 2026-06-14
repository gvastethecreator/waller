using Microsoft.UI.Xaml.Media;
using Waller.Native.Core.Models;

namespace Waller.Native.App.ViewModels;

internal static class PlacementPreview
{
    private const int OffsetAlignmentThreshold = 34;

    public static Stretch StretchFor(WallpaperPlacement placement)
    {
        ArgumentNullException.ThrowIfNull(placement);

        return DefinedEnumValue.Require(
            placement.FitMode,
            nameof(placement.FitMode),
            "Unknown preview fit mode.") switch
        {
            WallpaperFitMode.Cover => Stretch.UniformToFill,
            WallpaperFitMode.Contain => Stretch.Uniform,
            WallpaperFitMode.Stretch => Stretch.Fill,
            WallpaperFitMode.Center => Stretch.None,
            WallpaperFitMode.Tile => Stretch.UniformToFill,
            _ => InvalidFitMode(placement.FitMode),
        };
    }

    public static AlignmentX AlignmentXFor(WallpaperPlacement placement)
    {
        ArgumentNullException.ThrowIfNull(placement);
        DefinedEnumValue.Require(
            placement.Anchor,
            nameof(placement.Anchor),
            "Unknown preview anchor.");

        if (placement.OffsetXPercent <= -OffsetAlignmentThreshold)
        {
            return AlignmentX.Right;
        }

        if (placement.OffsetXPercent >= OffsetAlignmentThreshold)
        {
            return AlignmentX.Left;
        }

        return placement.Anchor switch
        {
            WallpaperAnchor.TopLeft or WallpaperAnchor.Left or WallpaperAnchor.BottomLeft => AlignmentX.Left,
            WallpaperAnchor.TopRight or WallpaperAnchor.Right or WallpaperAnchor.BottomRight => AlignmentX.Right,
            WallpaperAnchor.Top or WallpaperAnchor.Center or WallpaperAnchor.Bottom => AlignmentX.Center,
            _ => InvalidAlignmentXAnchor(placement.Anchor),
        };
    }

    public static AlignmentY AlignmentYFor(WallpaperPlacement placement)
    {
        ArgumentNullException.ThrowIfNull(placement);
        DefinedEnumValue.Require(
            placement.Anchor,
            nameof(placement.Anchor),
            "Unknown preview anchor.");

        if (placement.OffsetYPercent <= -OffsetAlignmentThreshold)
        {
            return AlignmentY.Bottom;
        }

        if (placement.OffsetYPercent >= OffsetAlignmentThreshold)
        {
            return AlignmentY.Top;
        }

        return placement.Anchor switch
        {
            WallpaperAnchor.TopLeft or WallpaperAnchor.Top or WallpaperAnchor.TopRight => AlignmentY.Top,
            WallpaperAnchor.BottomLeft or WallpaperAnchor.Bottom or WallpaperAnchor.BottomRight => AlignmentY.Bottom,
            WallpaperAnchor.Left or WallpaperAnchor.Center or WallpaperAnchor.Right => AlignmentY.Center,
            _ => InvalidAlignmentYAnchor(placement.Anchor),
        };
    }

    private static Stretch InvalidFitMode(WallpaperFitMode fitMode) =>
        throw new ArgumentOutOfRangeException(
            nameof(fitMode),
            fitMode,
            "Unknown preview fit mode.");

    private static AlignmentX InvalidAlignmentXAnchor(WallpaperAnchor anchor) =>
        throw new ArgumentOutOfRangeException(
            nameof(anchor),
            anchor,
            "Unknown preview anchor.");

    private static AlignmentY InvalidAlignmentYAnchor(WallpaperAnchor anchor) =>
        throw new ArgumentOutOfRangeException(
            nameof(anchor),
            anchor,
            "Unknown preview anchor.");
}
