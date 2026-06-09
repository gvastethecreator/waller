using Microsoft.UI.Xaml;
using Waller.Native.Core.Settings;

namespace Waller.Native.App.ViewModels;

internal static class ThemePreferenceMapper
{
    public static ElementTheme ToElementTheme(AppThemePreference preference) => preference switch
    {
        AppThemePreference.Light => ElementTheme.Light,
        AppThemePreference.Dark => ElementTheme.Dark,
        _ => ElementTheme.Default,
    };
}
