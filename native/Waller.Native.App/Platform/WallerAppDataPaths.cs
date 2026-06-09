namespace Waller.Native.App.Platform;

internal static class WallerAppDataPaths
{
    public static string Root { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "Waller");
}
