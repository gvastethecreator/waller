namespace Waller.Native.Core.Settings;

public readonly record struct WindowPlacement(int Width, int Height, int X, int Y);

public static class WindowPlacementPolicy
{
    private const int LegacyDefaultWindowWidth = 1120;
    private const int LegacyDefaultWindowHeight = 760;
    private const int InterimDefaultWindowWidth = 1520;
    private const int InterimDefaultWindowHeight = 960;

    public static WindowPlacement Resolve(
        UserSettings settings,
        int workAreaX,
        int workAreaY,
        int workAreaWidth,
        int workAreaHeight)
    {
        ArgumentNullException.ThrowIfNull(settings);

        if (workAreaWidth <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(workAreaWidth));
        }

        if (workAreaHeight <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(workAreaHeight));
        }

        var useCenteredDefault =
            settings.WindowX is null ||
            settings.WindowY is null ||
            (settings.WindowWidth == LegacyDefaultWindowWidth &&
             settings.WindowHeight == LegacyDefaultWindowHeight) ||
            (settings.WindowWidth == InterimDefaultWindowWidth &&
             settings.WindowHeight == InterimDefaultWindowHeight);

        var requestedWidth = useCenteredDefault
            ? UserSettingsPolicy.DefaultWindowWidth
            : settings.WindowWidth;
        var requestedHeight = useCenteredDefault
            ? UserSettingsPolicy.DefaultWindowHeight
            : settings.WindowHeight;
        var width = Math.Min(Math.Max(UserSettingsPolicy.MinWindowWidth, requestedWidth), workAreaWidth);
        var height = Math.Min(Math.Max(UserSettingsPolicy.MinWindowHeight, requestedHeight), workAreaHeight);

        if (!useCenteredDefault)
        {
            return new(width, height, settings.WindowX!.Value, settings.WindowY!.Value);
        }

        return new(
            width,
            height,
            workAreaX + ((workAreaWidth - width) / 2),
            workAreaY + ((workAreaHeight - height) / 2));
    }
}
