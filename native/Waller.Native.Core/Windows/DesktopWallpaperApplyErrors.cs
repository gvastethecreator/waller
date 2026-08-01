namespace Waller.Native.Core.Windows;

internal static class DesktopWallpaperApplyErrors
{
    public static bool IsRecoverable(Exception error)
    {
        ArgumentNullException.ThrowIfNull(error);
        return error is not OperationCanceledException;
    }
}
