using Waller.Native.Core.Models;
using Windows.Graphics.Imaging;
using Windows.Storage;

namespace Waller.Native.Core.Rendering;

internal static class ImageSourceDecoder
{
    public static async Task<PixelBuffer> DecodeAsync(
        string? imagePath,
        CancellationToken cancellationToken = default)
    {
        if (!WallpaperSourcePath.IsExistingImagePath(imagePath))
        {
            throw new WallpaperRenderException(
                ApplyErrorCodes.MissingImageSource,
                "Image source file does not exist.");
        }

        var file = await StorageFile.GetFileFromPathAsync(imagePath);
        cancellationToken.ThrowIfCancellationRequested();

        using var stream = await file.OpenReadAsync();
        var decoder = await BitmapDecoder.CreateAsync(stream);
        var pixelData = await decoder.GetPixelDataAsync(
            BitmapPixelFormat.Rgba8,
            BitmapAlphaMode.Ignore,
            new BitmapTransform(),
            ExifOrientationMode.IgnoreExifOrientation,
            ColorManagementMode.DoNotColorManage);
        cancellationToken.ThrowIfCancellationRequested();

        var rgba = pixelData.DetachPixelData();
        var width = checked((int)decoder.PixelWidth);
        var height = checked((int)decoder.PixelHeight);
        var rgb = new byte[checked(width * height * 3)];

        for (var source = 0; source < rgba.Length; source += 4)
        {
            var target = (source / 4) * 3;
            rgb[target] = rgba[source];
            rgb[target + 1] = rgba[source + 1];
            rgb[target + 2] = rgba[source + 2];
        }

        return new PixelBuffer(width, height, rgb);
    }
}
