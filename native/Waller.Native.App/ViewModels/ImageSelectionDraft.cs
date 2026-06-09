using Waller.Native.Core.Models;

namespace Waller.Native.App.ViewModels;

internal sealed record ImageSelectionDraft(string ImagePath, string DisplayFileName)
{
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
