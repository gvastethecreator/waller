using Microsoft.UI.Xaml;
using Waller.Native.Core.Models;
using Waller.Native.Core.Settings;

namespace Waller.Native.App.ViewModels;

internal static class ThemePreferenceMapper
{
    public static ElementTheme ToElementTheme(AppThemePreference preference)
    {
        return DefinedEnumValue.Require(
            preference,
            nameof(preference),
            "Unknown theme preference.") switch
        {
            AppThemePreference.System => ElementTheme.Default,
            AppThemePreference.Light => ElementTheme.Light,
            AppThemePreference.Dark => ElementTheme.Dark,
            _ => InvalidThemePreference(preference),
        };
    }

    private static ElementTheme InvalidThemePreference(AppThemePreference preference) =>
        throw new ArgumentOutOfRangeException(
            nameof(preference),
            preference,
            "Unknown theme preference.");
}
