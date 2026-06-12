using Waller.Native.Core.Models;
using Waller.Native.Core.Rendering;

namespace Waller.Native.App.ViewModels;

public sealed partial record LocalizedText
{
    public string SelectedSourceWarning(WallpaperSource source) =>
        WallpaperSourceFiles.IsMissingImageFile(source)
            ? $"{MissingSourcePrefix}: {source.ImagePath}"
            : string.Empty;

    public string ValidationMessage(ArgumentException error)
    {
        if (error is WallpaperSourcePathException pathError)
        {
            return pathError.ErrorCode switch
            {
                WallpaperSourcePathException.FullyQualifiedRequired => ImagePathMustBeFull,
                WallpaperSourcePathException.UnsupportedFileType => ImagePathUnsupportedFileType,
                _ => ImagePathRequired,
            };
        }

        return error.ParamName switch
        {
            "colorHex" => InvalidColor,
            "imagePath" => ImagePathRequired,
            _ => CheckValue,
        };
    }

    public string SourceKind(WallpaperSourceKind source) => source switch
    {
        WallpaperSourceKind.Empty => EmptySource,
        WallpaperSourceKind.Image => ImageSource,
        WallpaperSourceKind.SolidColor => ColorSource,
        _ => CheckValue,
    };

    public string FitMode(WallpaperFitMode fit) =>
        PlacementText.FitMode(fit, this);

    public string AnchorLabel(WallpaperAnchor anchor) =>
        PlacementText.AnchorLabel(anchor, this);
}
