using Microsoft.UI.Xaml;
using Waller.Native.App.Platform;
using Waller.Native.Core.Settings;
using Windows.Graphics;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Waller.Native.App;

/// <summary>
/// The application window. This hosts a Frame that displays pages. Add your
/// UI and logic to MainPage.xaml / MainPage.xaml.cs instead of here so you
/// can use Page features such as navigation events and the Loaded lifecycle.
/// </summary>
public sealed partial class MainWindow : Window
{
    private readonly UserSettingsStore settingsStore = WallerLocalDataStores.CreateDefault().Settings;

    public MainWindow()
    {
        InitializeComponent();

        ExtendsContentIntoTitleBar = true;
        SetTitleBar(AppTitleBar);

        AppWindow.SetIcon("Assets/AppIcon.ico");

        // Navigate the root frame to the main page on startup.
        RootFrame.Navigate(typeof(MainPage));

        _ = RestoreWindowPlacementAsync();
        Closed += async (_, _) => await SaveWindowPlacementAsync();
    }

    private async Task RestoreWindowPlacementAsync()
    {
        try
        {
            var settings = await settingsStore.LoadAsync();
            if (settings.WindowWidth > 0 && settings.WindowHeight > 0)
            {
                AppWindow.Resize(new SizeInt32(settings.WindowWidth, settings.WindowHeight));
            }

            if (settings.WindowX is int x && settings.WindowY is int y)
            {
                AppWindow.Move(new PointInt32(x, y));
            }
        }
        catch (Exception error) when (LocalDataErrorPolicy.IsRecoverableWindowPlacement(error))
        {
        }
    }

    private async Task SaveWindowPlacementAsync()
    {
        try
        {
            var settings = await settingsStore.LoadAsync();
            var position = AppWindow.Position;
            var size = AppWindow.Size;
            await settingsStore.SaveAsync(settings.WithWindowPlacement(
                size.Width,
                size.Height,
                position.X,
                position.Y));
        }
        catch (Exception error) when (LocalDataErrorPolicy.IsRecoverableWindowPlacement(error))
        {
        }
    }
}
