namespace Waller.Native.Core.Settings;

public static class UserSettingsPolicy
{
    public const int DefaultWindowWidth = 1120;
    public const int DefaultWindowHeight = 760;
    public const int MinWindowWidth = 640;
    public const int MinWindowHeight = 480;

    public static UserSettings Normalize(UserSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        var theme = Enum.IsDefined(settings.Theme)
            ? settings.Theme
            : UserSettings.Default.Theme;
        var windowX = settings.WindowX is null || settings.WindowY is null
            ? null
            : settings.WindowX;
        var windowY = settings.WindowX is null || settings.WindowY is null
            ? null
            : settings.WindowY;

        return settings with
        {
            Theme = theme,
            Language = AppLanguages.NormalizeOrDefault(settings.Language),
            WindowWidth = Math.Max(MinWindowWidth, settings.WindowWidth),
            WindowHeight = Math.Max(MinWindowHeight, settings.WindowHeight),
            WindowX = windowX,
            WindowY = windowY,
        };
    }
}
