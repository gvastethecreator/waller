using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Waller.Native.App.ViewModels;

namespace Waller.Native.App.Controls;

public sealed partial class SettingsModal : UserControl
{
    public static readonly DependencyProperty ViewModelProperty = DependencyProperty.Register(
        nameof(ViewModel),
        typeof(MainPageViewModel),
        typeof(SettingsModal),
        new PropertyMetadata(null));

    public SettingsModal()
    {
        InitializeComponent();
    }

    public MainPageViewModel? ViewModel
    {
        get => (MainPageViewModel?)GetValue(ViewModelProperty);
        set => SetValue(ViewModelProperty, value);
    }

    public void FocusTheme() =>
        SettingsThemeComboBox.Focus(FocusState.Programmatic);
}
