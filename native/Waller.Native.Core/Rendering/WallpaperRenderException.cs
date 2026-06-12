using Waller.Native.Core.Models;

namespace Waller.Native.Core.Rendering;

public sealed class WallpaperRenderException(string errorCode, string message, Exception? innerException = null)
    : InvalidOperationException(message, innerException)
{
    public string ErrorCode { get; } = ApplyErrorCodes.Normalize(errorCode);
}
