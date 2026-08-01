using Waller.Native.App.ViewModels;
using Waller.Native.Core.Models;
using Waller.Native.Core.Rendering;
using Waller.Native.Core.Sessions;
using Waller.Native.Core.Windows;
using Waller.Native.Workflows.Apply;
using Waller.Native.Workflows.MonitorEditing;
using Waller.Native.Workflows.Presets;
using Waller.Native.Workflows.Settings;
using Waller.Native.Workflows.Shell;
using Waller.Native.Workflows.Windowing;

namespace Waller.Native.App.Platform;

internal sealed class WallerAppComposition
{
    private WallerAppComposition(MainWindow window)
    {
        Window = window;
    }

    public MainWindow Window { get; }

    public static async Task<WallerAppComposition> CreateAsync(
        CancellationToken cancellationToken = default)
    {
        var localData = WallerLocalDataStores.CreateDefault();
        var userSettings = new UserSettingsWorkflow(localData.Settings);
        var presets = new PresetWorkflow(localData.Presets);
        var windowPlacement = new WindowPlacementWorkflow(userSettings);
        var window = new MainWindow(windowPlacement);
        var windowHandle = WinRT.Interop.WindowNative.GetWindowHandle(window);
        var renderer = new BasicPngWallpaperRenderer(localData.RenderedWallpapers);
        var workspace = new ShellWorkspace(ActiveSession.FromMonitors([]));
        var applyService = new WallpaperApplyService(renderer, new DesktopWallpaperApplier());
        var services = new WallerAppServices(
            new WindowsMonitorDetector(),
            new EmptyMonitorDetector(),
            new ImageFilePicker(windowHandle),
            new ApplyWorkflow(applyService, workspace),
            localData,
            new MonitorEditorWorkflow(),
            presets,
            userSettings,
            workspace);
        var viewModel = new MainPageViewModel(services);
        var page = new MainPage(viewModel);
        window.Attach(page);
        await window.RestorePlacementAsync(cancellationToken);

        return new WallerAppComposition(window);
    }
}
