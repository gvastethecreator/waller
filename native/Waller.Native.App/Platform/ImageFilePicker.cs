using Windows.Storage.Pickers;
using Waller.Native.Core.Models;

namespace Waller.Native.App.Platform;

public sealed class ImageFilePicker : IImageFilePicker
{
    private readonly nint ownerWindowHandle;

    public ImageFilePicker(nint ownerWindowHandle)
    {
        if (ownerWindowHandle == 0)
        {
            throw new ArgumentOutOfRangeException(nameof(ownerWindowHandle));
        }

        this.ownerWindowHandle = ownerWindowHandle;
    }

    public async Task<string?> PickImagePathAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var picker = new FileOpenPicker
        {
            SuggestedStartLocation = PickerLocationId.PicturesLibrary,
            ViewMode = PickerViewMode.Thumbnail,
        };

        foreach (var extension in WallpaperImageFileTypes.PickerExtensions)
        {
            picker.FileTypeFilter.Add(extension);
        }

        WinRT.Interop.InitializeWithWindow.Initialize(picker, ownerWindowHandle);

        var file = await picker.PickSingleFileAsync();
        return file?.Path;
    }
}
