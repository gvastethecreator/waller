namespace Waller.Native.Core.Models;

public sealed record ApplyResult(
    MonitorIdentity Monitor,
    bool Succeeded,
    string? ErrorCode,
    string? ErrorMessage)
{
    public static ApplyResult Success(MonitorIdentity monitor) => new(monitor, true, null, null);

    public static ApplyResult Failure(MonitorIdentity monitor, string? errorCode, string? errorMessage = null) =>
        new(
            monitor,
            false,
            ApplyErrorCodes.IsKnown(errorCode) ? errorCode : ApplyErrorCodes.WallpaperApplyFailed,
            errorMessage);
}
