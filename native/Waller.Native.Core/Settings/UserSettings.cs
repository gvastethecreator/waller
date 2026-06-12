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

    public UserSettings(
        AppThemePreference Theme,
        string? Language,
        int WindowWidth,
        int WindowHeight,
        int? WindowX,
        int? WindowY,
        Guid? LastSelectedPresetId)
    {
        this.Theme = Theme;
        language = Language ?? string.Empty;
        this.WindowWidth = WindowWidth;
        this.WindowHeight = WindowHeight;
        this.WindowX = WindowX;
        this.WindowY = WindowY;
        this.LastSelectedPresetId = LastSelectedPresetId;
    }

    public static UserSettings Default { get; } =
        new(
            AppThemePreference.System,
            AppLanguages.English,
            UserSettingsPolicy.DefaultWindowWidth,
            UserSettingsPolicy.DefaultWindowHeight,
            null,
            null,
            null);

    public AppThemePreference Theme { get; init; }

    public string Language
    {
        get => language;
        init => language = value ?? string.Empty;
    }

    public int WindowWidth { get; init; }

    public int WindowHeight { get; init; }

    public int? WindowX { get; init; }

    public int? WindowY { get; init; }

    public Guid? LastSelectedPresetId { get; init; }

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
        if (!Enum.IsDefined(theme))
        {
            throw new ArgumentOutOfRangeException(nameof(theme), theme, "Theme preference is not supported.");
        }

        var normalizedLanguage = AppLanguages.Normalize(language)
            ?? throw new ArgumentException("Settings language is not supported.", nameof(language));

        return this with
        {
            Theme = theme,
            Language = normalizedLanguage,
            LastSelectedPresetId = lastSelectedPresetId,
        };
    }

    public UserSettings WithLastSelectedPreset(Guid? lastSelectedPresetId) =>
        this with
        {
            LastSelectedPresetId = lastSelectedPresetId,
        };
}
