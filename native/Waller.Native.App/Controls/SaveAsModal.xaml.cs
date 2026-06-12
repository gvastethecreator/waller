using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Waller.Native.App.ViewModels;

namespace Waller.Native.App.Controls;

public sealed partial class SaveAsModal : UserControl
{
    public static readonly DependencyProperty ViewModelProperty = DependencyProperty.Register(
        nameof(ViewModel),
        typeof(MainPageViewModel),
        typeof(SaveAsModal),
        new PropertyMetadata(null));

    public SaveAsModal()
    {
        InitializeComponent();
    }

    public MainPageViewModel? ViewModel
    {
        get => (MainPageViewModel?)GetValue(ViewModelProperty);
        set => SetValue(ViewModelProperty, value);
    }

    public void FocusPresetName() =>
        SaveAsPresetNameTextBox.Focus(FocusState.Programmatic);
}
