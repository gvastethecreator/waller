namespace Waller.Native.Core.Models;

public sealed class WallpaperSourcePathException(string errorCode, string message)
    : ArgumentException(message, "imagePath")
{
    public const string Required = "image-path-required";
    public const string FullyQualifiedRequired = "image-path-fully-qualified-required";
    public const string UnsupportedFileType = "image-path-unsupported-file-type";

    public string ErrorCode { get; } = errorCode;
}
