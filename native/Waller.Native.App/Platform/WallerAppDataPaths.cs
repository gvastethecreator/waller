namespace Waller.Native.App.Platform;

internal static class WallerAppDataPaths
{
    public const string AppFolderName = "Waller";

    public static string Root { get; } = RootFor(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData));

    public static string RootFor(string localApplicationDataPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(localApplicationDataPath);
        return Path.Combine(localApplicationDataPath, AppFolderName);
    }
}
