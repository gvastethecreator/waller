using Microsoft.UI.Xaml;
using Microsoft.UI.Windowing;
using Waller.Native.Core.Settings;
using Waller.Native.Workflows.Settings;
using Waller.Native.Workflows.Windowing;
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
    private readonly WindowPlacementWorkflow windowPlacement;
    private Task? closeTask;
    private bool destroyRequested;

    internal MainWindow(WindowPlacementWorkflow windowPlacement)
    {
        ArgumentNullException.ThrowIfNull(windowPlacement);
        this.windowPlacement = windowPlacement;
        InitializeComponent();

        ExtendsContentIntoTitleBar = true;
        SetTitleBar(AppTitleBar);

        AppWindow.SetIcon("Assets/AppIcon.ico");

        AppWindow.Closing += OnAppWindowClosing;
    }

    internal void Attach(MainPage page)
    {
        ArgumentNullException.ThrowIfNull(page);
        if (RootFrame.Content is not null)
        {
            throw new InvalidOperationException("MainWindow content is already attached.");
        }

        RootFrame.Content = page;
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

    internal async Task RestorePlacementAsync(CancellationToken cancellationToken = default)
    {
        var workArea = DisplayArea
            .GetFromWindowId(AppWindow.Id, DisplayAreaFallback.Primary)
            .WorkArea;
        var placement = await windowPlacement.RestoreAsync(
            new WindowWorkArea(workArea.X, workArea.Y, workArea.Width, workArea.Height),
            cancellationToken);

        AppWindow.Resize(new SizeInt32(placement.Width, placement.Height));
        AppWindow.Move(new PointInt32(placement.X, placement.Y));
    }

    private async void OnAppWindowClosing(AppWindow sender, AppWindowClosingEventArgs args)
    {
        if (destroyRequested)
        {
            return;
        }

        args.Cancel = true;
        closeTask ??= SavePlacementAndDestroyAsync();
        await closeTask;
    }

    private async Task SavePlacementAndDestroyAsync()
    {
        var position = AppWindow.Position;
        var size = AppWindow.Size;
        var result = await windowPlacement.SaveAsync(
            new WindowPlacement(size.Width, size.Height, position.X, position.Y));

        if (result.Succeeded || result.Error == UserSettingsUpdateError.LocalStorageUnavailable)
        {
            destroyRequested = true;
            AppWindow.Destroy();
            Application.Current.Exit();
        }
    }
}
