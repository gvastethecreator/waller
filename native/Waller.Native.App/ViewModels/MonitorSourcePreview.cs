using Microsoft.UI;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Waller.Native.Core.Models;

namespace Waller.Native.App.ViewModels;

internal static class MonitorSourcePreview
{
    public static Brush BaseBrush(WallpaperSource source) => source.Kind switch
    {
        WallpaperSourceKind.SolidColor => ColorHex.BrushFromHex(source.ColorHex),
        WallpaperSourceKind.Empty => new SolidColorBrush(Colors.Black),
        WallpaperSourceKind.Image => new SolidColorBrush(Colors.Transparent),
        _ => new SolidColorBrush(Colors.Transparent),
    };

    public static ImageBrush? ImageBrush(WallpaperSource source, WallpaperPlacement placement)
    {
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
}
