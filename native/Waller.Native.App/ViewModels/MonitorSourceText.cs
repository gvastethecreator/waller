using Waller.Native.Core.Models;

namespace Waller.Native.App.ViewModels;

internal static class MonitorSourceText
{
    public static string Summary(WallpaperSource source, LocalizedText text)
    {
        return source.Kind switch
        {
            WallpaperSourceKind.Image when WallpaperSourceFiles.IsMissingImageFile(source) => text.MissingSource,
            WallpaperSourceKind.Image => WallpaperSourceFiles.ImageFileName(source) ?? text.ImageSource,
            WallpaperSourceKind.SolidColor => source.ColorHex ?? text.ColorSource,
            _ => text.EmptySource,
        };
    }
}
