namespace Waller.Native.Core.Settings;

public enum AppThemePreference
{
    System,
    Light,
    Dark,
}

public sealed record UserSettings(
    AppThemePreference Theme,
    string Language,
    int WindowWidth,
    int WindowHeight,
    int? WindowX,
    int? WindowY,
    Guid? LastSelectedPresetId)
{
    public static UserSettings Default { get; } =
        new(
            AppThemePreference.System,
            AppLanguages.English,
            UserSettingsPolicy.DefaultWindowWidth,
            UserSettingsPolicy.DefaultWindowHeight,
            null,
            null,
            null);

    public UserSettings WithWindowPlacement(int width, int height, int x, int y) =>
        this with
        {
            WindowWidth = width,
            WindowHeight = height,
            WindowX = x,
            WindowY = y,
        };

    public UserSettings WithPreferences(
        AppThemePreference theme,
        string language,
        Guid? lastSelectedPresetId) =>
        this with
        {
            Theme = theme,
            Language = language,
            LastSelectedPresetId = lastSelectedPresetId,
        };

    public UserSettings WithLastSelectedPreset(Guid? lastSelectedPresetId) =>
        this with
        {
            LastSelectedPresetId = lastSelectedPresetId,
        };
}
