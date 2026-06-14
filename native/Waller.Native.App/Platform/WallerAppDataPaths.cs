using System.Security.Principal;
using Microsoft.Win32;

namespace Waller.Native.App.Platform;

internal static class WallerAppDataPaths
{
    public const string AppFolderName = "Waller";

    public static string Root { get; } = RootFor(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData));

    public static string RenderedRoot { get; } = RootFor(UserVisibleLocalApplicationData());

    public static string RootFor(string localApplicationDataPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(localApplicationDataPath);
        return Path.Combine(localApplicationDataPath, AppFolderName);
    }

    private static string UserVisibleLocalApplicationData()
    {
        var profileDirectory = CurrentUserProfileDirectoryFromRegistry();
        if (!string.IsNullOrWhiteSpace(profileDirectory))
        {
            return Path.Combine(profileDirectory, "AppData", "Local");
        }

        var userProfileEnvironment = Environment.GetEnvironmentVariable("USERPROFILE");
        if (!string.IsNullOrWhiteSpace(userProfileEnvironment))
        {
            return Path.Combine(userProfileEnvironment, "AppData", "Local");
        }

        var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        if (!string.IsNullOrWhiteSpace(userProfile))
        {
            return Path.Combine(userProfile, "AppData", "Local");
        }

        return Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
    }

    private static string? CurrentUserProfileDirectoryFromRegistry()
    {
        var user = WindowsIdentity.GetCurrent().User;
        if (user is null)
        {
            return null;
        }

        var profileDirectory = Registry.GetValue(
            $@"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows NT\CurrentVersion\ProfileList\{user.Value}",
            "ProfileImagePath",
            null) as string;

        return string.IsNullOrWhiteSpace(profileDirectory)
            ? null
            : Environment.ExpandEnvironmentVariables(profileDirectory);
    }
}
