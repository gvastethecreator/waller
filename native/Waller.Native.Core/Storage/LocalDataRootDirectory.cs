namespace Waller.Native.Core.Storage;

internal static class LocalDataRootDirectory
{
    public static string RequireFullyQualified(string rootDirectory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(rootDirectory);
        if (!Path.IsPathFullyQualified(rootDirectory))
        {
            throw new ArgumentException("Local data root directory must be fully qualified.", nameof(rootDirectory));
        }

        return rootDirectory;
    }
}
