namespace Waller.Native.Core.Storage;

internal static class LocalDataFile
{
    public static void DeleteIfExists(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        try
        {
            File.Delete(path);
        }
        catch (DirectoryNotFoundException)
        {
        }
        catch (FileNotFoundException)
        {
        }
    }

    public static void DeleteRecoverableIfExists(string path)
    {
        _ = TryDeleteIfExists(path);
    }

    public static bool TryDeleteIfExists(string path)
    {
        try
        {
            DeleteIfExists(path);
            return true;
        }
        catch (Exception error) when (LocalDataFileSystemErrors.IsRecoverable(error))
        {
            return false;
        }
    }
}
