using Waller.Native.Core.Settings;

namespace Waller.Native.App.ViewModels;

internal sealed class SettingsSaveRequest(SettingsPreferenceDraft draft)
{
    public static SettingsSaveRequest FromSelection(
        AppThemePreference selectedTheme,
        string selectedLanguage,
        PresetMenuItem? selectedPreset) =>
        new(
            SettingsPreferenceDraft.FromSelection(
                selectedTheme,
                selectedLanguage,
                selectedPreset?.Id));

    public Guid? LastSelectedPresetId => draft.LastSelectedPresetId;

    public UserSettings ApplyTo(UserSettings settings) => draft.ApplyTo(settings);
}
