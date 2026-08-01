using Waller.Native.Core.Windows;
using Waller.Native.Workflows.Apply;
using Waller.Native.Workflows.MonitorEditing;
using Waller.Native.Workflows.Presets;
using Waller.Native.Workflows.Settings;
using Waller.Native.Workflows.Shell;

namespace Waller.Native.App.Platform;

internal sealed record WallerAppServices
{
    public WallerAppServices(
        IMonitorDetector PrimaryMonitorDetector,
        IMonitorDetector FallbackMonitorDetector,
        IImageFilePicker ImageFilePicker,
        ApplyWorkflow Apply,
        WallerLocalDataStores LocalData,
        MonitorEditorWorkflow MonitorEditor,
        PresetWorkflow Presets,
        UserSettingsWorkflow UserSettings,
        IShellWorkspace Workspace)
    {
        ArgumentNullException.ThrowIfNull(PrimaryMonitorDetector);
        ArgumentNullException.ThrowIfNull(FallbackMonitorDetector);
        ArgumentNullException.ThrowIfNull(ImageFilePicker);
        ArgumentNullException.ThrowIfNull(Apply);
        ArgumentNullException.ThrowIfNull(LocalData);
        ArgumentNullException.ThrowIfNull(MonitorEditor);
        ArgumentNullException.ThrowIfNull(Presets);
        ArgumentNullException.ThrowIfNull(UserSettings);
        ArgumentNullException.ThrowIfNull(Workspace);

        this.PrimaryMonitorDetector = PrimaryMonitorDetector;
        this.FallbackMonitorDetector = FallbackMonitorDetector;
        this.ImageFilePicker = ImageFilePicker;
        this.Apply = Apply;
        this.LocalData = LocalData;
        this.MonitorEditor = MonitorEditor;
        this.Presets = Presets;
        this.UserSettings = UserSettings;
        this.Workspace = Workspace;
    }

    public IMonitorDetector PrimaryMonitorDetector { get; }

    public IMonitorDetector FallbackMonitorDetector { get; }

    public IImageFilePicker ImageFilePicker { get; }

    public ApplyWorkflow Apply { get; }

    public WallerLocalDataStores LocalData { get; }

    public MonitorEditorWorkflow MonitorEditor { get; }

    public PresetWorkflow Presets { get; }

    public UserSettingsWorkflow UserSettings { get; }

    public IShellWorkspace Workspace { get; }

}
