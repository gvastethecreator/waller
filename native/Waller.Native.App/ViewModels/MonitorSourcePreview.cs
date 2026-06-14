using Microsoft.UI;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Waller.Native.Core.Models;

namespace Waller.Native.App.ViewModels;

internal static class MonitorSourcePreview
{
    public static Brush BaseBrush(WallpaperSource source)
    {
        ArgumentNullException.ThrowIfNull(source);

        return DefinedEnumValue.Require(
            source.Kind,
            nameof(source.Kind),
            "Unknown preview source kind.") switch
        {
            WallpaperSourceKind.SolidColor => ColorHex.BrushFromHex(source.ColorHex),
            WallpaperSourceKind.Empty => new SolidColorBrush(Colors.Black),
            WallpaperSourceKind.Image => new SolidColorBrush(Colors.Transparent),
            _ => InvalidSourceKind(source.Kind),
        };
    }

    public static ImageBrush? ImageBrush(WallpaperSource source, WallpaperPlacement placement)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(placement);

        if (!WallpaperSourceFiles.HasExistingImageFile(source))
        {
            return null;
        }

        return new ImageBrush
        {
            ImageSource = new BitmapImage(new Uri(source.ImagePath!)),
            Stretch = PlacementPreview.StretchFor(placement),
            AlignmentX = PlacementPreview.AlignmentXFor(placement),
            AlignmentY = PlacementPreview.AlignmentYFor(placement),
        };
    }

    private static Brush InvalidSourceKind(WallpaperSourceKind sourceKind) =>
        throw new ArgumentOutOfRangeException(
            nameof(sourceKind),
            sourceKind,
            "Unknown preview source kind.");
}
