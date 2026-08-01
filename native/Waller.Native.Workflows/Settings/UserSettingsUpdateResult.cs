using Waller.Native.Core.Settings;

namespace Waller.Native.Workflows.Settings;

public enum UserSettingsUpdateError
{
    None,
    LocalStorageUnavailable,
}

public sealed record UserSettingsUpdateResult
{
    private UserSettingsUpdateResult(
        UserSettings? updatedSettings,
        UserSettingsUpdateError error)
    {
        if ((updatedSettings is null) == (error == UserSettingsUpdateError.None))
        {
            throw new ArgumentException("A Settings update result must be either saved or failed.");
        }

        UpdatedSettings = updatedSettings;
        Error = error;
    }

    public UserSettings? UpdatedSettings { get; }

    public UserSettingsUpdateError Error { get; }

    public bool Succeeded => Error == UserSettingsUpdateError.None;

    public static UserSettingsUpdateResult Saved(UserSettings settings) =>
        new(settings ?? throw new ArgumentNullException(nameof(settings)), UserSettingsUpdateError.None);

    public static UserSettingsUpdateResult RecoverableFailure() =>
        new(null, UserSettingsUpdateError.LocalStorageUnavailable);

    public bool TryGetUpdatedSettings(out UserSettings settings)
    {
        settings = UpdatedSettings!;
        return Succeeded;
    }
}
