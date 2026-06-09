using Microsoft.UI.Xaml.Media;
using Waller.Native.Core.Models;

namespace Waller.Native.App.ViewModels;

internal static class PlacementPreview
{
    private const int OffsetAlignmentThreshold = 34;

    public static Stretch StretchFor(WallpaperPlacement placement) => placement.FitMode switch
    {
        WallpaperFitMode.Contain => Stretch.Uniform,
        WallpaperFitMode.Stretch => Stretch.Fill,
        WallpaperFitMode.Center => Stretch.None,
        _ => Stretch.UniformToFill,
    };

    public static AlignmentX AlignmentXFor(WallpaperPlacement placement)
    {
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
            _ => AlignmentX.Center,
        };
    }

    public static AlignmentY AlignmentYFor(WallpaperPlacement placement)
    {
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
            _ => AlignmentY.Center,
        };
    }
}
