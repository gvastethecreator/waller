using Waller.Native.Core.Models;

namespace Waller.Native.Core.Rendering;

internal sealed record ImagePlacementPlan
{
    private int drawWidth;
    private int drawHeight;

    public ImagePlacementPlan(bool IsTile, int OriginX, int OriginY, int DrawWidth, int DrawHeight)
    {
        ValidatePositiveDimension(DrawWidth, nameof(DrawWidth));
        ValidatePositiveDimension(DrawHeight, nameof(DrawHeight));

        this.IsTile = IsTile;
        this.OriginX = OriginX;
        this.OriginY = OriginY;
        drawWidth = DrawWidth;
        drawHeight = DrawHeight;
    }

    public bool IsTile { get; init; }

    public int OriginX { get; init; }

    public int OriginY { get; init; }

    public int DrawWidth
    {
        get => drawWidth;
        init
        {
            ValidatePositiveDimension(value, nameof(value));
            drawWidth = value;
        }
    }

    public int DrawHeight
    {
        get => drawHeight;
        init
        {
            ValidatePositiveDimension(value, nameof(value));
            drawHeight = value;
        }
    }

    public static ImagePlacementPlan Create(
        int sourceWidth,
        int sourceHeight,
        int targetWidth,
        int targetHeight,
        WallpaperPlacement placement)
    {
        ArgumentNullException.ThrowIfNull(placement);

        ValidatePositiveDimension(sourceWidth, nameof(sourceWidth));
        ValidatePositiveDimension(sourceHeight, nameof(sourceHeight));
        ValidatePositiveDimension(targetWidth, nameof(targetWidth));
        ValidatePositiveDimension(targetHeight, nameof(targetHeight));

        var fitMode = DefinedEnumValue.Require(
            placement.FitMode,
            nameof(placement.FitMode),
            "Unknown image placement fit mode.");
        if (fitMode == WallpaperFitMode.Tile)
        {
            return new ImagePlacementPlan(
                IsTile: true,
                OriginX: 0,
                OriginY: 0,
                DrawWidth: sourceWidth,
                DrawHeight: sourceHeight);
        }

        var scale = fitMode switch
        {
            WallpaperFitMode.Cover => GetUniformScale(sourceWidth, sourceHeight, targetWidth, targetHeight, useMax: true),
            WallpaperFitMode.Stretch => (ScaleX: targetWidth / (double)sourceWidth, ScaleY: targetHeight / (double)sourceHeight),
            WallpaperFitMode.Contain => GetUniformScale(sourceWidth, sourceHeight, targetWidth, targetHeight, useMax: false),
            WallpaperFitMode.Center => (ScaleX: 1d, ScaleY: 1d),
            _ => InvalidFitMode(fitMode),
        };

        var drawWidth = Math.Max(1, (int)Math.Round(sourceWidth * scale.ScaleX));
        var drawHeight = Math.Max(1, (int)Math.Round(sourceHeight * scale.ScaleY));
        var origin = GetAnchoredOrigin(
            targetWidth,
            targetHeight,
            drawWidth,
            drawHeight,
            DefinedEnumValue.Require(
                placement.Anchor,
                nameof(placement.Anchor),
                "Unknown image placement anchor."),
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

    private static (double ScaleX, double ScaleY) InvalidFitMode(WallpaperFitMode fitMode) =>
        throw new ArgumentOutOfRangeException(nameof(fitMode), fitMode, "Unknown image placement fit mode.");

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
            WallpaperAnchor.Top or WallpaperAnchor.Center or WallpaperAnchor.Bottom => (targetWidth - drawWidth) / 2,
            WallpaperAnchor.TopRight or WallpaperAnchor.Right or WallpaperAnchor.BottomRight => targetWidth - drawWidth,
            _ => InvalidAnchorX(anchor),
        };

        var y = anchor switch
        {
            WallpaperAnchor.TopLeft or WallpaperAnchor.Top or WallpaperAnchor.TopRight => 0,
            WallpaperAnchor.Left or WallpaperAnchor.Center or WallpaperAnchor.Right => (targetHeight - drawHeight) / 2,
            WallpaperAnchor.BottomLeft or WallpaperAnchor.Bottom or WallpaperAnchor.BottomRight => targetHeight - drawHeight,
            _ => InvalidAnchorY(anchor),
        };

        return (
            ApplyOffset(x, targetWidth, drawWidth, offsetXPercent),
            ApplyOffset(y, targetHeight, drawHeight, offsetYPercent));
    }

    private static int InvalidAnchorX(WallpaperAnchor anchor) =>
        throw new ArgumentOutOfRangeException(nameof(anchor), anchor, "Unknown image placement anchor.");

    private static int InvalidAnchorY(WallpaperAnchor anchor) =>
        throw new ArgumentOutOfRangeException(nameof(anchor), anchor, "Unknown image placement anchor.");

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
