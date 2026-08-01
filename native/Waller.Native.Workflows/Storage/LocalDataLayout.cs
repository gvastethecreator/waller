namespace Waller.Native.Workflows.Storage;

public sealed record LocalDataLayout
{
    public const string AppFolderName = "Waller";
    public const string RenderedCacheFolderName = ".waller";

    public LocalDataLayout(string appDataRoot, string renderedCacheRoot)
    {
        AppDataRoot = RequireFullyQualified(appDataRoot, nameof(appDataRoot));
        RenderedCacheRoot = RequireFullyQualified(renderedCacheRoot, nameof(renderedCacheRoot));
    }

    public string AppDataRoot { get; }

    public string RenderedCacheRoot { get; }

    public static LocalDataLayout Create(
        string localApplicationDataPath,
        string userProfilePath)
    {
        var localAppData = RequireFullyQualified(
            localApplicationDataPath,
            nameof(localApplicationDataPath));
        var userProfile = RequireFullyQualified(userProfilePath, nameof(userProfilePath));

        return new LocalDataLayout(
            Path.Combine(localAppData, AppFolderName),
            Path.Combine(userProfile, RenderedCacheFolderName));
    }

    private static string RequireFullyQualified(string path, string parameterName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path, parameterName);
        if (!Path.IsPathFullyQualified(path))
        {
            throw new ArgumentException("Local data paths must be fully qualified.", parameterName);
        }

        return Path.GetFullPath(path);
    }
}
