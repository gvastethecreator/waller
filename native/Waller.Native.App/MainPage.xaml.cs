using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Waller.Native.App.ViewModels;
using Windows.System;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Waller.Native.App;

/// <summary>
/// The main content page displayed inside the application window.
/// </summary>
public sealed partial class MainPage : Page
{
    public MainPageViewModel ViewModel { get; } = new();

    public MainPage()
    {
        InitializeComponent();
        ViewModel.PropertyChanged += (_, args) =>
        {
            if (args.PropertyName == nameof(ViewModel.ManagePresetsVisibility)
                && ViewModel.IsManagePresetsOpen)
            {
                FocusWhenReady(ManagePresetList);
            }

            if (args.PropertyName == nameof(ViewModel.SaveAsVisibility)
                && ViewModel.IsSaveAsOpen)
            {
                FocusWhenReady(SaveAsPresetNameTextBox);
            }

            if (args.PropertyName == nameof(ViewModel.SettingsVisibility)
                && ViewModel.IsSettingsOpen)
            {
                FocusWhenReady(SettingsThemeComboBox);
            }

            if (args.PropertyName == nameof(ViewModel.DeleteConfirmationVisibility)
                && ViewModel.IsDeleteConfirmationOpen)
            {
                FocusWhenReady(ConfirmDeletePresetButton);
            }
        };
        KeyDown += OnKeyDown;
        Loaded += OnLoaded;
    }

    private async void OnLoaded(object sender, RoutedEventArgs args)
    {
        try
        {
            await ViewModel.InitializeAsync();
        }
        catch (Exception)
        {
            ViewModel.ReportInitializationFailure();
        }
    }

    private void OnKeyDown(object sender, KeyRoutedEventArgs args)
    {
        if (args.Key != VirtualKey.Escape || !ViewModel.IsAnyModalOpen)
        {
            return;
        }

        ViewModel.CloseTopModalCommand.Execute(null);
        args.Handled = true;
    }

    private void FocusWhenReady(Control control)
    {
        DispatcherQueue.TryEnqueue(() =>
        {
            control.Focus(global::Microsoft.UI.Xaml.FocusState.Programmatic);
        });
    }
}
