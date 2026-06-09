namespace Waller.Native.Core.Storage;

public static class LocalDataFileSystemErrors
{
    public static bool IsRecoverable(Exception exception) =>
        exception is IOException or UnauthorizedAccessException;
}
