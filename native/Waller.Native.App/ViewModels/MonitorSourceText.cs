using Waller.Native.Core.Models;

namespace Waller.Native.App.ViewModels;

internal static class MonitorSourceText
{
    public static string Summary(WallpaperSource source, LocalizedText text)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(text);

        return DefinedEnumValue.Require(
            source.Kind,
            nameof(source.Kind),
            "Unknown monitor source kind.") switch
        {
            WallpaperSourceKind.Empty => text.EmptySource,
            WallpaperSourceKind.Image when WallpaperSourceFiles.IsMissingImageFile(source) => text.MissingSource,
            WallpaperSourceKind.Image => WallpaperSourceFiles.ImageFileName(source) ?? text.ImageSource,
            WallpaperSourceKind.SolidColor => source.ColorHex ?? text.ColorSource,
            _ => InvalidSourceKind(source.Kind),
        };
    }

    private static string InvalidSourceKind(WallpaperSourceKind sourceKind) =>
        throw new ArgumentOutOfRangeException(
            nameof(sourceKind),
            sourceKind,
            "Unknown monitor source kind.");
}
