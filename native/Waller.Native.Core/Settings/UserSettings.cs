using Waller.Native.Core.Models;

namespace Waller.Native.Core.Settings;

public enum AppThemePreference
{
    System,
    Light,
    Dark,
}

public sealed record UserSettings
{
    private string language = string.Empty;
    private Guid? lastSelectedPresetId;

    public UserSettings(
        AppThemePreference Theme,
        string? Language,
        int WindowWidth,
        int WindowHeight,
        int? WindowX,
        int? WindowY,
        Guid? LastSelectedPresetId,
        bool ThemePreferenceWasSet = false)
    {
        this.Theme = Theme;
        language = Language ?? string.Empty;
        this.WindowWidth = WindowWidth;
        this.WindowHeight = WindowHeight;
        this.WindowX = WindowX;
        this.WindowY = WindowY;
        this.LastSelectedPresetId = PresetIds.NormalizeOptional(LastSelectedPresetId);
        this.ThemePreferenceWasSet = ThemePreferenceWasSet;
    }

    public static UserSettings Default { get; } =
        new(
            AppThemePreference.Dark,
            AppLanguages.English,
            UserSettingsPolicy.DefaultWindowWidth,
            UserSettingsPolicy.DefaultWindowHeight,
            null,
            null,
            null,
            ThemePreferenceWasSet: false);

    public AppThemePreference Theme { get; init; }

    /// <summary>
    /// Tracks whether the user explicitly chose a theme. Older settings files
    /// do not contain this value and therefore migrate to the dark default.
    /// </summary>
    public bool ThemePreferenceWasSet { get; init; }

    public string Language
    {
        get => language;
        init => language = value ?? string.Empty;
    }

    public int WindowWidth { get; init; }

    public int WindowHeight { get; init; }

    public int? WindowX { get; init; }

    public int? WindowY { get; init; }

    public Guid? LastSelectedPresetId
    {
        get => lastSelectedPresetId;
        init => lastSelectedPresetId = PresetIds.NormalizeOptional(value);
    }

    public UserSettings WithWindowPlacement(int width, int height, int x, int y) =>
        this with
        {
            WindowWidth = Math.Max(UserSettingsPolicy.MinWindowWidth, width),
            WindowHeight = Math.Max(UserSettingsPolicy.MinWindowHeight, height),
            WindowX = x,
            WindowY = y,
        };

    public UserSettings WithPreferences(
        AppThemePreference theme,
        string language,
        Guid? lastSelectedPresetId)
    {
        DefinedEnumValue.Require(theme, nameof(theme), "Theme preference is not supported.");

        var normalizedLanguage = AppLanguages.Normalize(language)
            ?? throw new ArgumentException("Settings language is not supported.", nameof(language));

        return this with
        {
            Theme = theme,
            ThemePreferenceWasSet = true,
            Language = normalizedLanguage,
            LastSelectedPresetId = PresetIds.NormalizeOptional(lastSelectedPresetId),
        };
    }

    public UserSettings WithLastSelectedPreset(Guid? lastSelectedPresetId) =>
        this with
        {
            LastSelectedPresetId = PresetIds.NormalizeOptional(lastSelectedPresetId),
        };
}
