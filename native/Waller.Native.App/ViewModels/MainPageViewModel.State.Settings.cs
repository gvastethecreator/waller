using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using Waller.Native.Core.Settings;

namespace Waller.Native.App.ViewModels;

public sealed partial class MainPageViewModel
{
    public ObservableCollection<OptionItem<AppThemePreference>> ThemeOptions { get; } = [];

    public ObservableCollection<OptionItem<string>> LanguageOptions { get; } = [];

    [ObservableProperty]
    public partial AppThemePreference SelectedThemePreference { get; set; } = AppThemePreference.System;

    [ObservableProperty]
    public partial string SelectedLanguage { get; set; } = AppLanguages.English;

    [ObservableProperty]
    public partial OptionItem<AppThemePreference>? SelectedThemeOption { get; set; }

    [ObservableProperty]
    public partial OptionItem<string>? SelectedLanguageOption { get; set; }
}
