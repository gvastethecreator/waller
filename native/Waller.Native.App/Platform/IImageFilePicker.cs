namespace Waller.Native.App.Platform;

public interface IImageFilePicker
{
    Task<string?> PickImagePathAsync(CancellationToken cancellationToken = default);
}
