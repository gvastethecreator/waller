using Waller.Native.Core.Models;

namespace Waller.Native.App.ViewModels;

internal sealed record MonitorSourceSelection(
    WallpaperSourceKind SourceKind,
    string? ImagePath,
    string? ColorHex);

internal sealed record ImageSourceSelectionResult(
    MonitorSourceSelection? Selection,
    string StatusText);

internal static class MonitorSourceSelectionFactory
{
    public static ImageSourceSelectionResult FromPickedImage(
        string? imagePath,
        MonitorEditTextPresenter text)
    {
        var selection = ImageSelectionDraft.FromPickerPath(imagePath, out var error);
        if (error is not null)
        {
            return new(null, text.InvalidEditValue(error));
        }

        return selection is null
            ? new(null, text.ImageSelectionCancelled)
            : new(
                new MonitorSourceSelection(
                    WallpaperSourceKind.Image,
                    selection.ImagePath,
                    ColorHex: null),
                text.SelectedImage(selection.DisplayFileName));
    }

    public static MonitorSourceSelection FromSwatch(ColorSwatchOption swatch) =>
        new(
            WallpaperSourceKind.SolidColor,
            ImagePath: null,
            swatch.Hex);
}
