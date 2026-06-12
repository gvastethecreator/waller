using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Waller.Native.App.ViewModels;

namespace Waller.Native.App.Controls;

public sealed partial class StatusFooter : UserControl
{
    public static readonly DependencyProperty ViewModelProperty = DependencyProperty.Register(
        nameof(ViewModel),
        typeof(MainPageViewModel),
        typeof(StatusFooter),
        new PropertyMetadata(null));

    public StatusFooter()
    {
        InitializeComponent();
    }

    public MainPageViewModel? ViewModel
    {
        get => (MainPageViewModel?)GetValue(ViewModelProperty);
        set => SetValue(ViewModelProperty, value);
    }
}
