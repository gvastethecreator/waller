using Waller.Native.Core.Models;
using Waller.Native.Core.Settings;

namespace Waller.Native.App.ViewModels;

internal static class LocalizedOptionCatalog
{
    public static IReadOnlyList<OptionItem<AppThemePreference>> ThemeOptions(LocalizedText text)
    {
        var localizedText = RequireText(text);

        return
        [
            new(AppThemePreference.System, localizedText.ThemeSystem),
            new(AppThemePreference.Light, localizedText.ThemeLight),
            new(AppThemePreference.Dark, localizedText.ThemeDark),
        ];
    }

    public static IReadOnlyList<OptionItem<string>> LanguageOptions(LocalizedText text)
    {
        var localizedText = RequireText(text);

        return
        [
            new(AppLanguages.English, localizedText.LanguageEnglish),
            new(AppLanguages.Spanish, localizedText.LanguageSpanish),
        ];
    }

    public static IReadOnlyList<OptionItem<WallpaperSourceKind>> SourceOptions(LocalizedText text)
    {
        var localizedText = RequireText(text);

        return Enum.GetValues<WallpaperSourceKind>()
            .Select(source => new OptionItem<WallpaperSourceKind>(source, localizedText.SourceKind(source)))
            .ToArray();
    }

    public static IReadOnlyList<OptionItem<WallpaperFitMode>> FitOptions(LocalizedText text)
    {
        var localizedText = RequireText(text);

        return Enum.GetValues<WallpaperFitMode>()
            .Select(fit => new OptionItem<WallpaperFitMode>(fit, localizedText.FitMode(fit)))
            .ToArray();
    }

    public static IReadOnlyList<OptionItem<WallpaperAnchor>> AnchorOptions(LocalizedText text)
    {
        var localizedText = RequireText(text);

        return Enum.GetValues<WallpaperAnchor>()
            .Select(anchor => new OptionItem<WallpaperAnchor>(
                anchor,
                localizedText.AnchorLabel(anchor),
                AnchorGlyph(anchor)))
            .ToArray();
    }

    private static string AnchorGlyph(WallpaperAnchor anchor) => anchor switch
    {
        WallpaperAnchor.TopLeft => "↖",
        WallpaperAnchor.Top => "↑",
        WallpaperAnchor.TopRight => "↗",
        WallpaperAnchor.Left => "←",
        WallpaperAnchor.Center => "•",
        WallpaperAnchor.Right => "→",
        WallpaperAnchor.BottomLeft => "↙",
        WallpaperAnchor.Bottom => "↓",
        WallpaperAnchor.BottomRight => "↘",
        _ => throw new ArgumentOutOfRangeException(nameof(anchor), anchor, "Wallpaper anchor is not supported."),
    };

    private static LocalizedText RequireText(LocalizedText text)
    {
        ArgumentNullException.ThrowIfNull(text);

        return text;
    }
}
