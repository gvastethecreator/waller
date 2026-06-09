using Waller.Native.Core.Models;

namespace Waller.Native.Core.Rendering;

internal sealed record ImagePlacementPlan(
    bool IsTile,
    int OriginX,
    int OriginY,
    int DrawWidth,
    int DrawHeight)
{
    public static ImagePlacementPlan Create(
        int sourceWidth,
        int sourceHeight,
        int targetWidth,
        int targetHeight,
        WallpaperPlacement placement)
    {
        ValidatePositiveDimension(sourceWidth, nameof(sourceWidth));
        ValidatePositiveDimension(sourceHeight, nameof(sourceHeight));
        ValidatePositiveDimension(targetWidth, nameof(targetWidth));
        ValidatePositiveDimension(targetHeight, nameof(targetHeight));

        if (placement.FitMode == WallpaperFitMode.Tile)
        {
            return new ImagePlacementPlan(
                IsTile: true,
                OriginX: 0,
                OriginY: 0,
                DrawWidth: sourceWidth,
                DrawHeight: sourceHeight);
        }

        var scale = placement.FitMode switch
        {
            WallpaperFitMode.Stretch => (ScaleX: targetWidth / (double)sourceWidth, ScaleY: targetHeight / (double)sourceHeight),
            WallpaperFitMode.Contain => GetUniformScale(sourceWidth, sourceHeight, targetWidth, targetHeight, useMax: false),
            WallpaperFitMode.Center => (ScaleX: 1d, ScaleY: 1d),
            _ => GetUniformScale(sourceWidth, sourceHeight, targetWidth, targetHeight, useMax: true),
        };

        var drawWidth = Math.Max(1, (int)Math.Round(sourceWidth * scale.ScaleX));
        var drawHeight = Math.Max(1, (int)Math.Round(sourceHeight * scale.ScaleY));
        var origin = GetAnchoredOrigin(
            targetWidth,
            targetHeight,
            drawWidth,
            drawHeight,
            placement.Anchor,
            placement.OffsetXPercent,
            placement.OffsetYPercent);

        return new ImagePlacementPlan(
            IsTile: false,
            origin.X,
            origin.Y,
            drawWidth,
            drawHeight);
    }

    private static (double ScaleX, double ScaleY) GetUniformScale(
        int sourceWidth,
        int sourceHeight,
        int targetWidth,
        int targetHeight,
        bool useMax)
    {
        var scaleX = targetWidth / (double)sourceWidth;
        var scaleY = targetHeight / (double)sourceHeight;
        var scale = useMax ? Math.Max(scaleX, scaleY) : Math.Min(scaleX, scaleY);
        return (scale, scale);
    }

    private static void ValidatePositiveDimension(int value, string paramName)
    {
        if (value <= 0)
        {
            throw new ArgumentOutOfRangeException(paramName, value, "Image placement dimensions must be positive.");
        }
    }

    private static (int X, int Y) GetAnchoredOrigin(
        int targetWidth,
        int targetHeight,
        int drawWidth,
        int drawHeight,
        WallpaperAnchor anchor,
        int offsetXPercent,
        int offsetYPercent)
    {
        var x = anchor switch
        {
            WallpaperAnchor.TopLeft or WallpaperAnchor.Left or WallpaperAnchor.BottomLeft => 0,
            WallpaperAnchor.TopRight or WallpaperAnchor.Right or WallpaperAnchor.BottomRight => targetWidth - drawWidth,
            _ => (targetWidth - drawWidth) / 2,
        };

        var y = anchor switch
        {
            WallpaperAnchor.TopLeft or WallpaperAnchor.Top or WallpaperAnchor.TopRight => 0,
            WallpaperAnchor.BottomLeft or WallpaperAnchor.Bottom or WallpaperAnchor.BottomRight => targetHeight - drawHeight,
            _ => (targetHeight - drawHeight) / 2,
        };

        return (
            ApplyOffset(x, targetWidth, drawWidth, offsetXPercent),
            ApplyOffset(y, targetHeight, drawHeight, offsetYPercent));
    }

    private static int ApplyOffset(
        int anchoredOrigin,
        int targetSize,
        int drawSize,
        int offsetPercent)
    {
        if (offsetPercent == 0)
        {
            return anchoredOrigin;
        }

        var lowerBound = Math.Min(0, targetSize - drawSize);
        var upperBound = Math.Max(0, targetSize - drawSize);
        var range = upperBound - lowerBound;
        var clampedOffset = WallpaperPlacement.ClampOffset(offsetPercent);
        var nudge = (int)Math.Round(
            range * clampedOffset / 200d,
            MidpointRounding.AwayFromZero);
        return Math.Clamp(anchoredOrigin + nudge, lowerBound, upperBound);
    }
}
