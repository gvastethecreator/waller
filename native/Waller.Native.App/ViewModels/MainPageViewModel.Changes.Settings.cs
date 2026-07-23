using Waller.Native.Core.Settings;

namespace Waller.Native.App.ViewModels;

public sealed partial class MainPageViewModel
{
    private bool isSynchronizingThemeToggle;

    partial void OnSelectedThemePreferenceChanged(AppThemePreference value)
    {
        OnPropertyChanged(nameof(RequestedTheme));
        isSynchronizingThemeToggle = true;
        IsDarkThemeSetting = value == AppThemePreference.Dark;
        isSynchronizingThemeToggle = false;
        SelectedThemeOption = OptionItems.Select(ThemeOptions, value);
    }

    partial void OnSelectedThemeOptionChanged(OptionItem<AppThemePreference>? value)
    {
        if (value is not null)
        {
            SelectedThemePreference = value.Value;
        }
    }

    partial void OnIsDarkThemeSettingChanged(bool value)
    {
        if (isSynchronizingThemeToggle)
        {
            return;
        }

        var theme = value
            ? AppThemePreference.Dark
            : AppThemePreference.Light;

        if (SelectedThemePreference != theme)
        {
            SelectedThemePreference = theme;
        }
    }

    partial void OnSelectedLanguageOptionChanged(OptionItem<string>? value)
    {
        if (value is not null)
        {
            SelectedLanguage = value.Value;
        }
    }

    partial void OnSelectedLanguageChanged(string value)
    {
        RefreshSettingOptions();
        RefreshEditorOptions();
        var refresh = LocalizedSurfaceRefresh.Refresh(
            Presets,
            SelectedPreset,
            Monitors,
            MissingMonitors,
            Text);
        SelectedPreset = refresh.SelectedPreset;
        NotifyPropertiesChanged(ViewModelNotificationGroups.LanguageRefreshSurface);
    }
}
