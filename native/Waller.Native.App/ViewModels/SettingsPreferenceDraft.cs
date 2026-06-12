using Waller.Native.Core.Settings;

namespace Waller.Native.App.ViewModels;

internal sealed record SettingsPreferenceDraft
{
    public SettingsPreferenceDraft(
        AppThemePreference Theme,
        string Language,
        Guid? LastSelectedPresetId)
    {
        if (!Enum.IsDefined(Theme))
        {
            throw new ArgumentOutOfRangeException(nameof(Theme), Theme, "Unknown Settings theme preference.");
        }

        this.Theme = Theme;
        this.Language = AppLanguages.Normalize(Language)
            ?? throw new ArgumentException("Settings language must be supported.", nameof(Language));
        this.LastSelectedPresetId = LastSelectedPresetId;
    }

    public AppThemePreference Theme { get; }

    public string Language { get; }

    public Guid? LastSelectedPresetId { get; }

    public static SettingsPreferenceDraft From(UserSettings settings) =>
        new(
            (settings ?? throw new ArgumentNullException(nameof(settings))).Theme,
            settings.Language,
            settings.LastSelectedPresetId);

    public static SettingsPreferenceDraft FromSelection(
        AppThemePreference theme,
        string language,
        Guid? lastSelectedPresetId) =>
        new(theme, language, lastSelectedPresetId);

    public UserSettings ApplyTo(UserSettings settings) =>
        (settings ?? throw new ArgumentNullException(nameof(settings)))
            .WithPreferences(Theme, Language, LastSelectedPresetId);
}
