using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Waller.Native.App.ViewModels;

namespace Waller.Native.App.Controls;

public sealed partial class ShellHeader : UserControl
{
    public static readonly DependencyProperty ViewModelProperty = DependencyProperty.Register(
        nameof(ViewModel),
        typeof(MainPageViewModel),
        typeof(ShellHeader),
        new PropertyMetadata(null));

    public static readonly DependencyProperty PresetsProperty = DependencyProperty.Register(
        nameof(Presets),
        typeof(PresetsViewModel),
        typeof(ShellHeader),
        new PropertyMetadata(null));

    public static readonly DependencyProperty ApplyProperty = DependencyProperty.Register(
        nameof(Apply),
        typeof(ApplyViewModel),
        typeof(ShellHeader),
        new PropertyMetadata(null));

    public ShellHeader()
    {
        InitializeComponent();
    }

    public MainPageViewModel? ViewModel
    {
        get => (MainPageViewModel?)GetValue(ViewModelProperty);
        set => SetValue(ViewModelProperty, value);
    }

    public PresetsViewModel? Presets
    {
        get => (PresetsViewModel?)GetValue(PresetsProperty);
        set => SetValue(PresetsProperty, value);
    }

    public ApplyViewModel? Apply
    {
        get => (ApplyViewModel?)GetValue(ApplyProperty);
        set => SetValue(ApplyProperty, value);
    }
}
