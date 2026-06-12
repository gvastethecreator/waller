using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Waller.Native.App.ViewModels;

namespace Waller.Native.App.Controls;

public sealed partial class ManagePresetsModal : UserControl
{
    public static readonly DependencyProperty ViewModelProperty = DependencyProperty.Register(
        nameof(ViewModel),
        typeof(MainPageViewModel),
        typeof(ManagePresetsModal),
        new PropertyMetadata(null));

    public ManagePresetsModal()
    {
        InitializeComponent();
    }

    public MainPageViewModel? ViewModel
    {
        get => (MainPageViewModel?)GetValue(ViewModelProperty);
        set => SetValue(ViewModelProperty, value);
    }

    public void FocusPresetList() =>
        ManagePresetList.Focus(FocusState.Programmatic);

    public void FocusConfirmDelete() =>
        ConfirmDeletePresetButton.Focus(FocusState.Programmatic);
}
