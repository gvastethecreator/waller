using System.Windows.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Waller.Native.App.ViewModels;

namespace Waller.Native.App.Controls;

public sealed partial class MissingMonitorRow : UserControl
{
    public static readonly DependencyProperty RowProperty = DependencyProperty.Register(
        nameof(Row),
        typeof(MissingMonitorRowViewModel),
        typeof(MissingMonitorRow),
        new PropertyMetadata(null));

    public static readonly DependencyProperty ReassignCommandProperty = DependencyProperty.Register(
        nameof(ReassignCommand),
        typeof(ICommand),
        typeof(MissingMonitorRow),
        new PropertyMetadata(null));

    public static readonly DependencyProperty ForgetCommandProperty = DependencyProperty.Register(
        nameof(ForgetCommand),
        typeof(ICommand),
        typeof(MissingMonitorRow),
        new PropertyMetadata(null));

    public static readonly DependencyProperty CanEditProperty = DependencyProperty.Register(
        nameof(CanEdit),
        typeof(bool),
        typeof(MissingMonitorRow),
        new PropertyMetadata(false));

    public static readonly DependencyProperty ReassignTextProperty = DependencyProperty.Register(
        nameof(ReassignText),
        typeof(string),
        typeof(MissingMonitorRow),
        new PropertyMetadata(string.Empty));

    public static readonly DependencyProperty ForgetTextProperty = DependencyProperty.Register(
        nameof(ForgetText),
        typeof(string),
        typeof(MissingMonitorRow),
        new PropertyMetadata(string.Empty));

    public MissingMonitorRow()
    {
        InitializeComponent();
    }

    public MissingMonitorRowViewModel? Row
    {
        get => (MissingMonitorRowViewModel?)GetValue(RowProperty);
        set => SetValue(RowProperty, value);
    }

    public ICommand? ReassignCommand
    {
        get => (ICommand?)GetValue(ReassignCommandProperty);
        set => SetValue(ReassignCommandProperty, value);
    }

    public ICommand? ForgetCommand
    {
        get => (ICommand?)GetValue(ForgetCommandProperty);
        set => SetValue(ForgetCommandProperty, value);
    }

    public bool CanEdit
    {
        get => (bool)GetValue(CanEditProperty);
        set => SetValue(CanEditProperty, value);
    }

    public string ReassignText
    {
        get => (string)GetValue(ReassignTextProperty);
        set => SetValue(ReassignTextProperty, value);
    }

    public string ForgetText
    {
        get => (string)GetValue(ForgetTextProperty);
        set => SetValue(ForgetTextProperty, value);
    }
}
