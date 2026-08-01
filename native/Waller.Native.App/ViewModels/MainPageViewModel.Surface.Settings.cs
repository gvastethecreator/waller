using Microsoft.UI.Xaml;

namespace Waller.Native.App.ViewModels;

public sealed partial class MainPageViewModel
{
    public ElementTheme RequestedTheme => ThemePreferenceMapper.ToElementTheme(SelectedThemePreference);
}
