using Microsoft.UI.Xaml;
using Microsoft.UI.Windowing;
using Waller.Native.App.Platform;
using Waller.Native.Core.Settings;
using Windows.Graphics;
using Windows.UI;

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
        RootFrame.Navigated += OnRootFrameNavigated;
        RootFrame.Navigate(typeof(MainPage));

        _ = RestoreWindowPlacementAsync();
        Closed += async (_, _) => await SaveWindowPlacementAsync();
    }

    private void OnRootFrameNavigated(object sender, Microsoft.UI.Xaml.Navigation.NavigationEventArgs args)
    {
        if (RootFrame.Content is not MainPage page)
        {
            return;
        }

        page.RequestedThemeChanged += OnRequestedThemeChanged;
        ApplyWindowTheme(page.ViewModel.RequestedTheme);
    }

    private void OnRequestedThemeChanged(ElementTheme theme) =>
        ApplyWindowTheme(theme);

    private void ApplyWindowTheme(ElementTheme theme)
    {
        RootLayout.RequestedTheme = theme;

        var isDark = theme == ElementTheme.Dark;
        var background = isDark
            ? Color.FromArgb(255, 32, 32, 32)
            : Color.FromArgb(255, 250, 250, 250);
        var foreground = isDark
            ? Color.FromArgb(255, 245, 245, 245)
            : Color.FromArgb(255, 31, 31, 31);
        var hover = isDark
            ? Color.FromArgb(255, 56, 56, 56)
            : Color.FromArgb(255, 235, 235, 235);
        var pressed = isDark
            ? Color.FromArgb(255, 72, 72, 72)
            : Color.FromArgb(255, 220, 220, 220);

        var titleBar = AppWindow.TitleBar;
        titleBar.BackgroundColor = background;
        titleBar.ForegroundColor = foreground;
        titleBar.ButtonBackgroundColor = background;
        titleBar.ButtonForegroundColor = foreground;
        titleBar.ButtonHoverBackgroundColor = hover;
        titleBar.ButtonHoverForegroundColor = foreground;
        titleBar.ButtonPressedBackgroundColor = pressed;
        titleBar.ButtonPressedForegroundColor = foreground;
    }

    private async Task RestoreWindowPlacementAsync()
    {
        try
        {
            var settings = await settingsStore.LoadAsync();
            var workArea = DisplayArea
                .GetFromWindowId(AppWindow.Id, DisplayAreaFallback.Primary)
                .WorkArea;
            var placement = WindowPlacementPolicy.Resolve(
                settings,
                workArea.X,
                workArea.Y,
                workArea.Width,
                workArea.Height);

            AppWindow.Resize(new SizeInt32(placement.Width, placement.Height));
            AppWindow.Move(new PointInt32(placement.X, placement.Y));
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
