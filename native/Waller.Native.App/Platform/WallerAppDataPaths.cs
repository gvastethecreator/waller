using System.Security.Principal;
using Microsoft.Win32;

namespace Waller.Native.App.Platform;

internal static class WallerAppDataPaths
{
    public const string AppFolderName = "Waller";

    public static string Root { get; } = RootFor(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData));

    public static string RenderedRoot { get; } = Path.Combine(UserVisibleProfileDirectory(), ".waller");

    public static string RootFor(string localApplicationDataPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(localApplicationDataPath);
        return Path.Combine(localApplicationDataPath, AppFolderName);
    }

    private static string UserVisibleProfileDirectory()
    {
        var localApplicationData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var packageUserProfile = UserProfileFromPackageLocalApplicationData(localApplicationData);
        if (!string.IsNullOrWhiteSpace(packageUserProfile))
        {
            return packageUserProfile;
        }

        var profileDirectory = CurrentUserProfileDirectoryFromRegistry();
        if (!string.IsNullOrWhiteSpace(profileDirectory))
        {
            return profileDirectory;
        }

        var userProfileEnvironment = Environment.GetEnvironmentVariable("USERPROFILE");
        if (!string.IsNullOrWhiteSpace(userProfileEnvironment))
        {
            return userProfileEnvironment;
        }

        var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        if (!string.IsNullOrWhiteSpace(userProfile))
        {
            return userProfile;
        }

        return Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
    }

    private static string? UserProfileFromPackageLocalApplicationData(string localApplicationDataPath)
    {
        if (string.IsNullOrWhiteSpace(localApplicationDataPath))
        {
            return null;
        }

        var directory = new DirectoryInfo(localApplicationDataPath);
        var localCache = directory.Parent;
        var packageDirectory = localCache?.Parent;
        var packagesDirectory = packageDirectory?.Parent;

        if (!directory.Name.Equals("Local", StringComparison.OrdinalIgnoreCase)
            || localCache?.Name.Equals("LocalCache", StringComparison.OrdinalIgnoreCase) != true
            || packagesDirectory?.Name.Equals("Packages", StringComparison.OrdinalIgnoreCase) != true)
        {
            return null;
        }

        var localData = packagesDirectory.Parent;
        var appData = localData?.Parent;
        return appData?.Parent?.FullName;
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
