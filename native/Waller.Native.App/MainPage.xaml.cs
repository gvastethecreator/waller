using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using System.ComponentModel;
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
        ViewModel.PropertyChanged += OnViewModelPropertyChanged;
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

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs args)
    {
        switch (args.PropertyName)
        {
            case nameof(ViewModel.ManagePresetsVisibility) when ViewModel.IsManagePresetsOpen:
                FocusWhenReady(ManagePresetsModal.FocusPresetList);
                break;
            case nameof(ViewModel.SaveAsVisibility) when ViewModel.IsSaveAsOpen:
                FocusWhenReady(SaveAsModal.FocusPresetName);
                break;
            case nameof(ViewModel.SettingsVisibility) when ViewModel.IsSettingsOpen:
                FocusWhenReady(SettingsModal.FocusTheme);
                break;
            case nameof(ViewModel.DeleteConfirmationVisibility) when ViewModel.IsDeleteConfirmationOpen:
                FocusWhenReady(ManagePresetsModal.FocusConfirmDelete);
                break;
        }
    }

    private void FocusWhenReady(Action focusAction)
    {
        DispatcherQueue.TryEnqueue(() => focusAction());
    }
}
