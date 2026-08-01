using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Waller.Native.App.ViewModels;

namespace Waller.Native.App.Controls;

public sealed partial class EditPanel : UserControl
{
    public static readonly DependencyProperty ViewModelProperty = DependencyProperty.Register(
        nameof(ViewModel),
        typeof(MonitorEditorViewModel),
        typeof(EditPanel),
        new PropertyMetadata(null));

    public EditPanel()
    {
        InitializeComponent();
    }

    public MonitorEditorViewModel? ViewModel
    {
        get => (MonitorEditorViewModel?)GetValue(ViewModelProperty);
        set => SetValue(ViewModelProperty, value);
    }
}
