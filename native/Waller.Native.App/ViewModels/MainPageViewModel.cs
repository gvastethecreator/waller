using CommunityToolkit.Mvvm.ComponentModel;
using Waller.Native.App.Platform;
using Waller.Native.Core.Models;
using Waller.Native.Core.Settings;
using Waller.Native.Core.Windows;
using Waller.Native.Workflows.Apply;
using Waller.Native.Workflows.MonitorEditing;
using Waller.Native.Workflows.Presets;
using Waller.Native.Workflows.Settings;
using Waller.Native.Workflows.Shell;

namespace Waller.Native.App.ViewModels;

public sealed partial class MainPageViewModel : ObservableObject
{
    private readonly IMonitorDetector primaryMonitorDetector;
    private readonly IMonitorDetector fallbackMonitorDetector;
    private readonly MainPageLocalState localState;
    private readonly IShellWorkspace workspace;
    private readonly ShellStatusTextPresenter shellText;

    internal MainPageViewModel(WallerAppServices services)
        : this(
            services.PrimaryMonitorDetector,
            services.FallbackMonitorDetector,
            services.ImageFilePicker,
            services.Apply,
            services.LocalData,
            services.MonitorEditor,
            services.Presets,
            services.UserSettings,
            services.Workspace)
    {
    }

    internal MainPageViewModel(
        IMonitorDetector primaryMonitorDetector,
        IMonitorDetector fallbackMonitorDetector,
        IImageFilePicker imageFilePicker,
        ApplyWorkflow applyWorkflow,
        WallerLocalDataStores localData,
        MonitorEditorWorkflow monitorEditorWorkflow,
        PresetWorkflow presetWorkflow,
        UserSettingsWorkflow userSettings,
        IShellWorkspace workspace)
    {
        ArgumentNullException.ThrowIfNull(primaryMonitorDetector);
        ArgumentNullException.ThrowIfNull(fallbackMonitorDetector);
        ArgumentNullException.ThrowIfNull(imageFilePicker);
        ArgumentNullException.ThrowIfNull(applyWorkflow);
        ArgumentNullException.ThrowIfNull(localData);
        ArgumentNullException.ThrowIfNull(monitorEditorWorkflow);
        ArgumentNullException.ThrowIfNull(presetWorkflow);
        ArgumentNullException.ThrowIfNull(userSettings);
        ArgumentNullException.ThrowIfNull(workspace);

        this.primaryMonitorDetector = primaryMonitorDetector;
        this.fallbackMonitorDetector = fallbackMonitorDetector;
        this.workspace = workspace;
        localState = new MainPageLocalState(localData, userSettings);
        shellText = new ShellStatusTextPresenter(() => Text);
        Apply = new ApplyViewModel(
            applyWorkflow,
            workspace,
            () => Text,
            status => StatusText = status,
            RefreshSessionSurface,
            NotifyCommandStateChanged);
        Editor = new MonitorEditorViewModel(
            monitorEditorWorkflow,
            imageFilePicker,
            workspace,
            Monitors,
            () => Text,
            status => StatusText = status,
            RefreshSessionSurface);
        Presets = new PresetsViewModel(
            presetWorkflow,
            userSettings,
            workspace,
            () => Text,
            status => StatusText = status,
            RefreshSessionSurface,
            NotifySessionSummaryChanged,
            NotifyModalStateChanged);
        RefreshSettingOptions();
    }

    private ActiveSession activeSession => workspace.ActiveSession;

    public ApplyViewModel Apply { get; }

    public MonitorEditorViewModel Editor { get; }

    public PresetsViewModel Presets { get; }
}
