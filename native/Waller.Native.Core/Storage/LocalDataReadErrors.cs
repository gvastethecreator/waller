using System.Text.Json;

namespace Waller.Native.Core.Storage;

internal static class LocalDataReadErrors
{
    public static bool IsRecoverable(Exception exception) =>
        exception is JsonException or NotSupportedException
        || IsRecoverableFileSystem(exception);

    public static bool IsRecoverableFileSystem(Exception exception) =>
        LocalDataFileSystemErrors.IsRecoverable(exception);
}
