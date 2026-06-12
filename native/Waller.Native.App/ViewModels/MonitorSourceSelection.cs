using Waller.Native.Core.Models;

namespace Waller.Native.App.ViewModels;

internal sealed record MonitorSourceSelection
{
    private WallpaperSourceKind sourceKind;
    private string? imagePath;
    private string? colorHex;

    public MonitorSourceSelection(
        WallpaperSourceKind SourceKind,
        string? ImagePath,
        string? ColorHex)
    {
        ValidateSourceKind(SourceKind, nameof(SourceKind));

        sourceKind = SourceKind;
        imagePath = SourceKind == WallpaperSourceKind.Image
            ? WallpaperSourcePath.NormalizeImagePath(ImagePath ?? string.Empty)
            : null;
        colorHex = SourceKind == WallpaperSourceKind.SolidColor
            ? ColorHexValue.Normalize(ColorHex ?? string.Empty)
            : null;
    }

    public WallpaperSourceKind SourceKind => sourceKind;

    public string? ImagePath => imagePath;

    public string? ColorHex => colorHex;

    private static void ValidateSourceKind(WallpaperSourceKind sourceKind, string parameterName)
    {
        if (!Enum.IsDefined(sourceKind))
        {
            throw new ArgumentOutOfRangeException(parameterName, sourceKind, "Unknown source kind.");
        }
    }
}

internal sealed record ImageSourceSelectionResult
{
    public ImageSourceSelectionResult(
        MonitorSourceSelection? Selection,
        string StatusText)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(StatusText);

        this.Selection = Selection;
        this.StatusText = StatusText;
    }

    public MonitorSourceSelection? Selection { get; }

    public string StatusText { get; }
}

internal static class MonitorSourceSelectionFactory
{
    public static ImageSourceSelectionResult FromPickedImage(
        string? imagePath,
        MonitorEditTextPresenter text)
    {
        ArgumentNullException.ThrowIfNull(text);

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
            (swatch ?? throw new ArgumentNullException(nameof(swatch))).Hex);
}
