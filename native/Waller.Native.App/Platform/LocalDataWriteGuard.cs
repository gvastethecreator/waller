namespace Waller.Native.App.Platform;

internal static class LocalDataWriteGuard
{
    public static async Task<T> TryAsync<T>(
        Func<Task<T>> write,
        T fallback)
    {
        try
        {
            return await write();
        }
        catch (Exception error) when (IsRecoverable(error))
        {
            return fallback;
        }
    }

    public static bool IsRecoverable(Exception error) =>
        LocalDataErrorPolicy.IsRecoverableFileSystem(error);
}
