using System.Windows.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Waller.Native.App.ViewModels;

namespace Waller.Native.App.Controls;

public sealed partial class MonitorRow : UserControl
{
    public static readonly DependencyProperty RowProperty = DependencyProperty.Register(
        nameof(Row),
        typeof(MonitorRowViewModel),
        typeof(MonitorRow),
        new PropertyMetadata(null));

    public static readonly DependencyProperty EditCommandProperty = DependencyProperty.Register(
        nameof(EditCommand),
        typeof(ICommand),
        typeof(MonitorRow),
        new PropertyMetadata(null));

    public static readonly DependencyProperty ApplyCommandProperty = DependencyProperty.Register(
        nameof(ApplyCommand),
        typeof(ICommand),
        typeof(MonitorRow),
        new PropertyMetadata(null));

    public static readonly DependencyProperty CanApplyProperty = DependencyProperty.Register(
        nameof(CanApply),
        typeof(bool),
        typeof(MonitorRow),
        new PropertyMetadata(false));

    public static readonly DependencyProperty EditTextProperty = DependencyProperty.Register(
        nameof(EditText),
        typeof(string),
        typeof(MonitorRow),
        new PropertyMetadata(string.Empty));

    public static readonly DependencyProperty ApplyTextProperty = DependencyProperty.Register(
        nameof(ApplyText),
        typeof(string),
        typeof(MonitorRow),
        new PropertyMetadata(string.Empty));

    public MonitorRow()
    {
        InitializeComponent();
    }

    public MonitorRowViewModel? Row
    {
        get => (MonitorRowViewModel?)GetValue(RowProperty);
        set => SetValue(RowProperty, value);
    }

    public ICommand? EditCommand
    {
        get => (ICommand?)GetValue(EditCommandProperty);
        set => SetValue(EditCommandProperty, value);
    }

    public ICommand? ApplyCommand
    {
        get => (ICommand?)GetValue(ApplyCommandProperty);
        set => SetValue(ApplyCommandProperty, value);
    }

    public bool CanApply
    {
        get => (bool)GetValue(CanApplyProperty);
        set => SetValue(CanApplyProperty, value);
    }

    public string EditText
    {
        get => (string)GetValue(EditTextProperty);
        set => SetValue(EditTextProperty, value);
    }

    public string ApplyText
    {
        get => (string)GetValue(ApplyTextProperty);
        set => SetValue(ApplyTextProperty, value);
    }
}
