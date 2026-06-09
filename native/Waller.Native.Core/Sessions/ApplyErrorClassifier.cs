using Waller.Native.Core.Models;
using Waller.Native.Core.Rendering;

namespace Waller.Native.Core.Sessions;

public static class ApplyErrorClassifier
{
    public static string FriendlyErrorCode(string? errorCode) =>
        ApplyErrorCodes.IsKnown(errorCode)
            ? errorCode!
            : ApplyErrorCodes.WallpaperApplyFailed;

    public static string FriendlyErrorCode(Exception error) => error switch
    {
        WallpaperRenderException renderError => FriendlyErrorCode(renderError.ErrorCode),
        _ => ApplyErrorCodes.WallpaperApplyFailed,
    };
}
