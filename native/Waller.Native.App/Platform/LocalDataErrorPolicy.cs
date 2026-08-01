using Waller.Native.Core.Storage;

namespace Waller.Native.App.Platform;

internal static class LocalDataErrorPolicy
{
    public static bool IsRecoverableFileSystem(Exception error) =>
        LocalDataFileSystemErrors.IsRecoverable(error);

    public static bool IsRecoverableWindowPlacement(Exception error) =>
        IsRecoverableFileSystem(error) || error is ArgumentException;
}
