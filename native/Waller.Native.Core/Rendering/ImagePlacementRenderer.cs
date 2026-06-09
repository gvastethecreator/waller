using Waller.Native.Core.Models;

namespace Waller.Native.Core.Rendering;

internal static class ImagePlacementRenderer
{
    public static PixelBuffer Render(
        PixelBuffer source,
        int targetWidth,
        int targetHeight,
        WallpaperPlacement placement)
    {
        var target = PixelBuffer.CreateSolid(targetWidth, targetHeight, RgbColor.Black);
        if (source.Width <= 0 || source.Height <= 0)
        {
            return target;
        }

        var plan = ImagePlacementPlan.Create(
            source.Width,
            source.Height,
            targetWidth,
            targetHeight,
            placement);

        if (plan.IsTile)
        {
            RenderTile(source, target);
            return target;
        }

        RenderScaled(source, target, plan);
        return target;
    }

    private static void RenderTile(PixelBuffer source, PixelBuffer target)
    {
        for (var y = 0; y < target.Height; y++)
        {
            for (var x = 0; x < target.Width; x++)
            {
                target.SetPixel(x, y, source.GetPixel(x % source.Width, y % source.Height));
            }
        }
    }

    private static void RenderScaled(
        PixelBuffer source,
        PixelBuffer target,
        ImagePlacementPlan plan)
    {
        for (var y = 0; y < target.Height; y++)
        {
            var drawY = y - plan.OriginY;
            if (drawY < 0 || drawY >= plan.DrawHeight)
            {
                continue;
            }

            var sourceY = Math.Clamp((int)(drawY * (source.Height / (double)plan.DrawHeight)), 0, source.Height - 1);
            for (var x = 0; x < target.Width; x++)
            {
                var drawX = x - plan.OriginX;
                if (drawX < 0 || drawX >= plan.DrawWidth)
                {
                    continue;
                }

                var sourceX = Math.Clamp((int)(drawX * (source.Width / (double)plan.DrawWidth)), 0, source.Width - 1);
                target.SetPixel(x, y, source.GetPixel(sourceX, sourceY));
            }
        }
    }
}
