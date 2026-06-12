using Waller.Native.Core.Serialization;
using Waller.Native.Core.Storage;

namespace Waller.Native.Core.Settings;

public sealed class UserSettingsStore(string rootDirectory)
{
    private readonly string settingsPath = Path.Combine(
        LocalDataRootDirectory.RequireFullyQualified(rootDirectory),
        "settings.json");

    public async Task<UserSettings> LoadAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            if (!File.Exists(settingsPath))
            {
                return UserSettings.Default;
            }

            var settings = await LocalJsonFile.ReadRecoverableAsync(
                settingsPath,
                WallerJsonContext.Default.UserSettings,
                cancellationToken);
            return UserSettingsPolicy.Normalize(settings ?? UserSettings.Default);
        }
        catch (Exception exception) when (LocalDataReadErrors.IsRecoverableFileSystem(exception))
        {
            return UserSettings.Default;
        }
    }

    public async Task SaveAsync(UserSettings settings, CancellationToken cancellationToken = default)
    {
        await LocalJsonFile.WriteAsync(
            settingsPath,
            UserSettingsPolicy.Normalize(settings),
            WallerJsonContext.Default.UserSettings,
            cancellationToken);
    }

}
