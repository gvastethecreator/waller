using Waller.Native.Core.Models;

namespace Waller.Native.App.ViewModels;

internal static class PlacementText
{
    public static string Summary(WallpaperPlacement placement, bool isSpanish)
    {
        var summary = $"{FitMode(placement.FitMode, isSpanish)} - {AnchorLabel(placement.Anchor, isSpanish)}";
        return placement.OffsetXPercent == 0 && placement.OffsetYPercent == 0
            ? summary
            : $"{summary} - {OffsetSummary(placement, isSpanish)}";
    }

    public static string FitMode(WallpaperFitMode fit, bool isSpanish) => fit switch
    {
        WallpaperFitMode.Cover => isSpanish ? "Cubrir" : "Cover",
        WallpaperFitMode.Contain => isSpanish ? "Contener" : "Contain",
        WallpaperFitMode.Stretch => isSpanish ? "Estirar" : "Stretch",
        WallpaperFitMode.Center => isSpanish ? "Centrar" : "Center",
        WallpaperFitMode.Tile => isSpanish ? "Mosaico" : "Tile",
        _ => UnsupportedValue(isSpanish),
    };

    public static string AnchorLabel(WallpaperAnchor anchor, bool isSpanish) => anchor switch
    {
        WallpaperAnchor.TopLeft => isSpanish ? "Arriba izquierda" : "Top left",
        WallpaperAnchor.Top => isSpanish ? "Arriba" : "Top",
        WallpaperAnchor.TopRight => isSpanish ? "Arriba derecha" : "Top right",
        WallpaperAnchor.Left => isSpanish ? "Izquierda" : "Left",
        WallpaperAnchor.Center => isSpanish ? "Centro" : "Center",
        WallpaperAnchor.Right => isSpanish ? "Derecha" : "Right",
        WallpaperAnchor.BottomLeft => isSpanish ? "Abajo izquierda" : "Bottom left",
        WallpaperAnchor.Bottom => isSpanish ? "Abajo" : "Bottom",
        WallpaperAnchor.BottomRight => isSpanish ? "Abajo derecha" : "Bottom right",
        _ => UnsupportedValue(isSpanish),
    };

    private static string UnsupportedValue(bool isSpanish) =>
        isSpanish ? "Valor no compatible" : "Unsupported value";

    private static string OffsetSummary(WallpaperPlacement placement, bool isSpanish) =>
        isSpanish
            ? $"Posicion {placement.OffsetXPercent}, {placement.OffsetYPercent}"
            : $"Position {placement.OffsetXPercent}, {placement.OffsetYPercent}";
}
