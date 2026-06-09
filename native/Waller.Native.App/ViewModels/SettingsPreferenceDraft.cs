using Waller.Native.Core.Settings;

namespace Waller.Native.App.ViewModels;

internal sealed record SettingsPreferenceDraft(
    AppThemePreference Theme,
    string Language,
    Guid? LastSelectedPresetId)
{
    public static SettingsPreferenceDraft From(UserSettings settings) =>
        new(settings.Theme, settings.Language, settings.LastSelectedPresetId);

    public static SettingsPreferenceDraft FromSelection(
        AppThemePreference theme,
        string language,
        Guid? lastSelectedPresetId) =>
        new(theme, language, lastSelectedPresetId);

    public UserSettings ApplyTo(UserSettings settings) =>
        settings.WithPreferences(Theme, Language, LastSelectedPresetId);
}
