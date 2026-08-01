using Waller.Native.Core.Models;

namespace Waller.Native.App.ViewModels;

internal sealed record ImageSelectionDraft
{
    private string imagePath = string.Empty;
    private string displayFileName = string.Empty;

    public ImageSelectionDraft(string ImagePath, string DisplayFileName)
    {
        imagePath = WallpaperSourcePath.NormalizeImagePath(ImagePath);
        displayFileName = ImageDisplayName.Normalize(DisplayFileName, nameof(DisplayFileName));
    }

    public string ImagePath
    {
        get => imagePath;
        init => imagePath = WallpaperSourcePath.NormalizeImagePath(value);
    }

    public string DisplayFileName
    {
        get => displayFileName;
        init => displayFileName = ImageDisplayName.Normalize(value, nameof(value));
    }

    public static ImageSelectionDraft? FromPickerPath(
        string? imagePath,
        out WallpaperSourcePathException? error)
    {
        error = null;
        if (string.IsNullOrWhiteSpace(imagePath))
        {
            return null;
        }

        if (!WallpaperSourcePath.TryNormalizeImagePath(imagePath, out var normalized, out error))
        {
            return null;
        }

        var displayFileName = WallpaperSourcePath.FileName(normalized) ?? normalized;
        return new ImageSelectionDraft(normalized, displayFileName);
    }
}
