using Waller.Native.Core.Models;
using Waller.Native.Core.Settings;

namespace Waller.Native.App.ViewModels;

internal static class LocalizedOptionCatalog
{
    public static IEnumerable<OptionItem<AppThemePreference>> ThemeOptions(LocalizedText text) =>
    [
        new(AppThemePreference.System, text.ThemeSystem),
        new(AppThemePreference.Light, text.ThemeLight),
        new(AppThemePreference.Dark, text.ThemeDark),
    ];

    public static IEnumerable<OptionItem<string>> LanguageOptions(LocalizedText text) =>
    [
        new(AppLanguages.English, text.LanguageEnglish),
        new(AppLanguages.Spanish, text.LanguageSpanish),
    ];

    public static IEnumerable<OptionItem<WallpaperSourceKind>> SourceOptions(LocalizedText text) =>
        Enum.GetValues<WallpaperSourceKind>()
            .Select(source => new OptionItem<WallpaperSourceKind>(source, text.SourceKind(source)));

    public static IEnumerable<OptionItem<WallpaperFitMode>> FitOptions(LocalizedText text) =>
        Enum.GetValues<WallpaperFitMode>()
            .Select(fit => new OptionItem<WallpaperFitMode>(fit, text.FitMode(fit)));

    public static IEnumerable<OptionItem<WallpaperAnchor>> AnchorOptions(LocalizedText text) =>
        Enum.GetValues<WallpaperAnchor>()
            .Select(anchor => new OptionItem<WallpaperAnchor>(anchor, text.AnchorLabel(anchor)));
}
