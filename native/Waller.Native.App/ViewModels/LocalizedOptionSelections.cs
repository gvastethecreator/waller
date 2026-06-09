using System.Collections.ObjectModel;
using Waller.Native.Core.Models;
using Waller.Native.Core.Settings;

namespace Waller.Native.App.ViewModels;

internal static class LocalizedOptionSelections
{
    public static SettingsOptionSelection RefreshSettings(
        ObservableCollection<OptionItem<AppThemePreference>> themeOptions,
        ObservableCollection<OptionItem<string>> languageOptions,
        LocalizedText text,
        AppThemePreference selectedTheme,
        string selectedLanguage) =>
        new(
            OptionItems.ReplaceAndSelect(
                themeOptions,
                LocalizedOptionCatalog.ThemeOptions(text),
                selectedTheme),
            OptionItems.ReplaceAndSelect(
                languageOptions,
                LocalizedOptionCatalog.LanguageOptions(text),
                selectedLanguage,
                StringComparer.OrdinalIgnoreCase));

    public static EditorOptionSelection RefreshEditor(
        ObservableCollection<OptionItem<WallpaperSourceKind>> sourceOptions,
        ObservableCollection<OptionItem<WallpaperFitMode>> fitOptions,
        ObservableCollection<OptionItem<WallpaperAnchor>> anchorOptions,
        LocalizedText text,
        WallpaperSourceKind selectedSource,
        WallpaperFitMode selectedFit,
        WallpaperAnchor selectedAnchor) =>
        new(
            OptionItems.ReplaceAndSelect(
                sourceOptions,
                LocalizedOptionCatalog.SourceOptions(text),
                selectedSource),
            OptionItems.ReplaceAndSelect(
                fitOptions,
                LocalizedOptionCatalog.FitOptions(text),
                selectedFit),
            OptionItems.ReplaceAndSelect(
                anchorOptions,
                LocalizedOptionCatalog.AnchorOptions(text),
                selectedAnchor));

    public static EditorOptionSelection SelectEditor(
        IEnumerable<OptionItem<WallpaperSourceKind>> sourceOptions,
        IEnumerable<OptionItem<WallpaperFitMode>> fitOptions,
        IEnumerable<OptionItem<WallpaperAnchor>> anchorOptions,
        WallpaperSourceKind selectedSource,
        WallpaperFitMode selectedFit,
        WallpaperAnchor selectedAnchor) =>
        new(
            OptionItems.Select(sourceOptions, selectedSource),
            OptionItems.Select(fitOptions, selectedFit),
            OptionItems.Select(anchorOptions, selectedAnchor));
}

internal sealed record SettingsOptionSelection(
    OptionItem<AppThemePreference>? Theme,
    OptionItem<string>? Language);

internal sealed record EditorOptionSelection(
    OptionItem<WallpaperSourceKind>? Source,
    OptionItem<WallpaperFitMode>? Fit,
    OptionItem<WallpaperAnchor>? Anchor);
