using Microsoft.UI.Xaml;

namespace Waller.Native.App.ViewModels;

internal static class VisibilityStates
{
    public static Visibility When(bool condition) =>
        condition ? Visibility.Visible : Visibility.Collapsed;

    public static Visibility Unless(bool condition) =>
        condition ? Visibility.Collapsed : Visibility.Visible;
}
