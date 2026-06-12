using CommunityToolkit.Mvvm.ComponentModel;
using Waller.Native.App.Platform;
using Waller.Native.Core.Models;
using Waller.Native.Core.Presets;
using Waller.Native.Core.Sessions;
using Waller.Native.Core.Settings;
using Waller.Native.Core.Windows;

namespace Waller.Native.App.ViewModels;

public sealed partial class MainPageViewModel : ObservableObject
{
    private readonly IMonitorDetector primaryMonitorDetector;
    private readonly IMonitorDetector fallbackMonitorDetector;
    private readonly IImageFilePicker imageFilePicker;
    private readonly WallpaperApplyService applyService;
    private readonly MainPageLocalState localState;
    private readonly PresetMatcher presetMatcher = new();
    private readonly ActiveSessionEditor sessionEditor = new();
    private readonly ApplyRunState applyRunState = new();
    private readonly MainPageTextPresenters textPresenters;
    private ActiveSession activeSession = ActiveSession.FromMonitors([]);
    private Preset? selectedPresetRecord;
    private bool isChangingPresetSelection;
    private int selectedPresetLoadVersion;
    private bool isRefreshingEditor;
    private bool isRefreshingColor;
    private Guid? lastSelectedPresetId;
    private PresetDeleteConfirmation? pendingDeletePreset;

    public MainPageViewModel()
        : this(WallerAppServices.CreateDefault())
    {
    }

    private MainPageViewModel(WallerAppServices services)
        : this(
            services.PrimaryMonitorDetector,
            services.FallbackMonitorDetector,
            services.ImageFilePicker,
            services.ApplyService,
            services.LocalData)
    {
    }

    internal MainPageViewModel(
        IMonitorDetector primaryMonitorDetector,
        IMonitorDetector fallbackMonitorDetector,
        IImageFilePicker imageFilePicker,
        WallpaperApplyService applyService,
        WallerLocalDataStores localData)
    {
        ArgumentNullException.ThrowIfNull(primaryMonitorDetector);
        ArgumentNullException.ThrowIfNull(fallbackMonitorDetector);
        ArgumentNullException.ThrowIfNull(imageFilePicker);
        ArgumentNullException.ThrowIfNull(applyService);
        ArgumentNullException.ThrowIfNull(localData);

        this.primaryMonitorDetector = primaryMonitorDetector;
        this.fallbackMonitorDetector = fallbackMonitorDetector;
        this.imageFilePicker = imageFilePicker;
        this.applyService = applyService;
        localState = new MainPageLocalState(localData);
        textPresenters = new MainPageTextPresenters(() => Text);
        RefreshSettingOptions();
        RefreshEditorOptions();
    }
}
