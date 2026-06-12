using Waller.Native.Core.Settings;

namespace Waller.Native.App.ViewModels;

public sealed partial class MainPageViewModel
{
    partial void OnSelectedThemePreferenceChanged(AppThemePreference value)
    {
        OnPropertyChanged(nameof(RequestedTheme));
        SelectedThemeOption = OptionItems.Select(ThemeOptions, value);
    }

    partial void OnSelectedThemeOptionChanged(OptionItem<AppThemePreference>? value)
    {
        if (value is not null)
        {
            SelectedThemePreference = value.Value;
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
