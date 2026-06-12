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
        string selectedLanguage)
    {
        ArgumentNullException.ThrowIfNull(themeOptions);
        ArgumentNullException.ThrowIfNull(languageOptions);
        ArgumentNullException.ThrowIfNull(text);
        if (!Enum.IsDefined(selectedTheme))
        {
            throw new ArgumentOutOfRangeException(nameof(selectedTheme), selectedTheme, "Unknown selected Settings theme.");
        }

        return new(
            OptionItems.ReplaceAndSelect(
                themeOptions,
                LocalizedOptionCatalog.ThemeOptions(text),
                selectedTheme),
            OptionItems.ReplaceAndSelect(
                languageOptions,
                LocalizedOptionCatalog.LanguageOptions(text),
                selectedLanguage,
                StringComparer.OrdinalIgnoreCase));
    }

    public static EditorOptionSelection RefreshEditor(
        ObservableCollection<OptionItem<WallpaperSourceKind>> sourceOptions,
        ObservableCollection<OptionItem<WallpaperFitMode>> fitOptions,
        ObservableCollection<OptionItem<WallpaperAnchor>> anchorOptions,
        LocalizedText text,
        WallpaperSourceKind selectedSource,
        WallpaperFitMode selectedFit,
        WallpaperAnchor selectedAnchor)
    {
        ValidateEditorSelection(selectedSource, selectedFit, selectedAnchor);
        ArgumentNullException.ThrowIfNull(sourceOptions);
        ArgumentNullException.ThrowIfNull(fitOptions);
        ArgumentNullException.ThrowIfNull(anchorOptions);
        ArgumentNullException.ThrowIfNull(text);

        return new(
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
    }

    public static EditorOptionSelection SelectEditor(
        IEnumerable<OptionItem<WallpaperSourceKind>> sourceOptions,
        IEnumerable<OptionItem<WallpaperFitMode>> fitOptions,
        IEnumerable<OptionItem<WallpaperAnchor>> anchorOptions,
        WallpaperSourceKind selectedSource,
        WallpaperFitMode selectedFit,
        WallpaperAnchor selectedAnchor)
    {
        ValidateEditorSelection(selectedSource, selectedFit, selectedAnchor);

        return new(
            OptionItems.Select(sourceOptions, selectedSource),
            OptionItems.Select(fitOptions, selectedFit),
            OptionItems.Select(anchorOptions, selectedAnchor));
    }

    private static void ValidateEditorSelection(
        WallpaperSourceKind selectedSource,
        WallpaperFitMode selectedFit,
        WallpaperAnchor selectedAnchor)
    {
        if (!Enum.IsDefined(selectedSource))
        {
            throw new ArgumentOutOfRangeException(nameof(selectedSource), selectedSource, "Unknown selected editor source.");
        }

        if (!Enum.IsDefined(selectedFit))
        {
            throw new ArgumentOutOfRangeException(nameof(selectedFit), selectedFit, "Unknown selected editor fit.");
        }

        if (!Enum.IsDefined(selectedAnchor))
        {
            throw new ArgumentOutOfRangeException(nameof(selectedAnchor), selectedAnchor, "Unknown selected editor anchor.");
        }
    }
}

internal sealed record SettingsOptionSelection
{
    public SettingsOptionSelection(
        OptionItem<AppThemePreference>? Theme,
        OptionItem<string>? Language)
    {
        this.Theme = Theme;
        this.Language = Language;
    }

    public OptionItem<AppThemePreference>? Theme { get; }

    public OptionItem<string>? Language { get; }
}

internal sealed record EditorOptionSelection
{
    public EditorOptionSelection(
        OptionItem<WallpaperSourceKind>? Source,
        OptionItem<WallpaperFitMode>? Fit,
        OptionItem<WallpaperAnchor>? Anchor)
    {
        this.Source = Source;
        this.Fit = Fit;
        this.Anchor = Anchor;
    }

    public OptionItem<WallpaperSourceKind>? Source { get; }

    public OptionItem<WallpaperFitMode>? Fit { get; }

    public OptionItem<WallpaperAnchor>? Anchor { get; }
}
