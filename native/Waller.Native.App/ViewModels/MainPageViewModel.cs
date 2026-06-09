using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Xaml;
using Waller.Native.App.Platform;
using Waller.Native.Core.Models;
using Waller.Native.Core.Presets;
using Waller.Native.Core.Rendering;
using Waller.Native.Core.Sessions;
using Waller.Native.Core.Settings;
using Waller.Native.Core.Windows;
using Windows.UI;

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
        this.primaryMonitorDetector = primaryMonitorDetector;
        this.fallbackMonitorDetector = fallbackMonitorDetector;
        this.imageFilePicker = imageFilePicker;
        this.applyService = applyService;
        localState = new MainPageLocalState(localData);
        textPresenters = new MainPageTextPresenters(() => Text);
        RefreshSettingOptions();
        RefreshEditorOptions();
    }

    public ObservableCollection<MonitorRowViewModel> Monitors { get; } = [];

    public ObservableCollection<MissingMonitorRowViewModel> MissingMonitors { get; } = [];

    public ObservableCollection<PresetMenuItem> Presets { get; } = [];

    public ObservableCollection<PresetMenuItem> ManagePresetItems { get; } = [];

    public ObservableCollection<OptionItem<WallpaperSourceKind>> SourceOptions { get; } = [];

    public ObservableCollection<OptionItem<WallpaperFitMode>> FitOptions { get; } = [];

    public ObservableCollection<OptionItem<WallpaperAnchor>> AnchorOptions { get; } = [];

    public ObservableCollection<ColorSwatchOption> ColorSwatches { get; } =
        new(ColorSwatchCatalog.Defaults());

    public ObservableCollection<OptionItem<AppThemePreference>> ThemeOptions { get; } = [];

    public ObservableCollection<OptionItem<string>> LanguageOptions { get; } = [];

    [ObservableProperty]
    public partial PresetMenuItem? SelectedPreset { get; set; }

    [ObservableProperty]
    public partial string PresetNameDraft { get; set; } = string.Empty;

    [ObservableProperty]
    public partial PresetMenuItem? SelectedManagePreset { get; set; }

    [ObservableProperty]
    public partial string ManagePresetNameDraft { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string SaveAsPresetNameDraft { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool IsSaveAsOpen { get; set; }

    [ObservableProperty]
    public partial bool IsManagePresetsOpen { get; set; }

    [ObservableProperty]
    public partial bool IsDeleteConfirmationOpen { get; set; }

    [ObservableProperty]
    public partial bool IsSettingsOpen { get; set; }

    [ObservableProperty]
    public partial AppThemePreference SelectedThemePreference { get; set; } = AppThemePreference.System;

    [ObservableProperty]
    public partial string SelectedLanguage { get; set; } = AppLanguages.English;

    [ObservableProperty]
    public partial OptionItem<AppThemePreference>? SelectedThemeOption { get; set; }

    [ObservableProperty]
    public partial OptionItem<string>? SelectedLanguageOption { get; set; }

    [ObservableProperty]
    public partial MonitorRowViewModel? SelectedMonitor { get; set; }

    [ObservableProperty]
    public partial OptionItem<WallpaperSourceKind>? SelectedSourceOption { get; set; }

    [ObservableProperty]
    public partial OptionItem<WallpaperFitMode>? SelectedFitOption { get; set; }

    [ObservableProperty]
    public partial OptionItem<WallpaperAnchor>? SelectedAnchorOption { get; set; }

    [ObservableProperty]
    public partial WallpaperSourceKind EditSourceKind { get; set; } = WallpaperSourceKind.Empty;

    [ObservableProperty]
    public partial string EditImagePath { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string EditColorHex { get; set; } = "#000000";

    [ObservableProperty]
    public partial Color EditColor { get; set; } = Color.FromArgb(255, 0, 0, 0);

    [ObservableProperty]
    public partial WallpaperFitMode EditFitMode { get; set; } = WallpaperFitMode.Cover;

    [ObservableProperty]
    public partial WallpaperAnchor EditAnchor { get; set; } = WallpaperAnchor.Center;

    [ObservableProperty]
    public partial double EditOffsetXPercent { get; set; }

    [ObservableProperty]
    public partial double EditOffsetYPercent { get; set; }

    [ObservableProperty]
    public partial string StatusText { get; set; } = LocalizedText.English.LoadedCurrentSetup;

    [ObservableProperty]
    public partial bool IsApplying { get; set; }

    [ObservableProperty]
    public partial string ApplyProgressText { get; set; } = string.Empty;

    [ObservableProperty]
    public partial double TopologyWidth { get; set; } = 720;

    [ObservableProperty]
    public partial double TopologyHeight { get; set; } = 96;

    public LocalizedText Text => LocalizedText.For(SelectedLanguage);

    public Visibility EditPanelVisibility =>
        MonitorEditorSurface.EditPanelVisibility(SelectedMonitor);

    public Visibility ApplyProgressVisibility => VisibilityStates.When(IsApplying);

    private ShellInteractionState InteractionState => new(
        IsApplying,
        IsSaveAsOpen,
        IsManagePresetsOpen,
        IsDeleteConfirmationOpen,
        IsSettingsOpen);

    private ApplyTextPresenter applyText => textPresenters.Apply;

    private PresetTextPresenter presetText => textPresenters.Preset;

    private MonitorEditTextPresenter monitorEditText => textPresenters.MonitorEdit;

    private ShellStatusTextPresenter shellText => textPresenters.Shell;

    public bool CanStartApply => InteractionState.CanStartApply;

    public bool CanEditSession => InteractionState.CanEditSession;

    public bool CanEditMonitorAssignment => InteractionState.CanEditMonitorAssignment;

    public bool CanEditPlacement => CanEditMonitorAssignment && EditSourceKind == WallpaperSourceKind.Image;

    public bool CanUseShellCommands => InteractionState.CanUseShellCommands;

    public bool CanMutateManagedPresets => InteractionState.CanMutateManagedPresets;

    public bool CanUseModalActions => InteractionState.CanUseModalActions;

    public bool IsAnyModalOpen => InteractionState.IsAnyModalOpen;

    public Visibility NoMonitorsVisibility =>
        MonitorRowsSurface.NoMonitorsVisibility(Monitors);

    public Visibility TopologyVisibility =>
        MonitorRowsSurface.TopologyVisibility(Monitors);

    public Visibility MissingMonitorsVisibility =>
        MonitorRowsSurface.MissingMonitorsVisibility(MissingMonitors);

    public Visibility ManagePresetEmptyVisibility =>
        VisibilityStates.When(ManagePresetItems.Count == 0);

    public string SelectedMonitorDisplayName =>
        MonitorEditorSurface.SelectedMonitorDisplayName(SelectedMonitor, Text);

    public string SessionSummary => Text.SessionSummary(
        activeSession.BasedOnPreset,
        activeSession.HasUnsavedPresetChanges,
        activeSession.MissingAssignments.Count,
        SelectedPreset);

    public Visibility ManagePresetsVisibility => VisibilityStates.When(IsManagePresetsOpen);

    public Visibility SaveAsVisibility => VisibilityStates.When(IsSaveAsOpen);

    public Visibility DeleteConfirmationVisibility => VisibilityStates.When(IsDeleteConfirmationOpen);

    public string DeleteConfirmationMessage =>
        pendingDeletePreset?.Message(Text) ?? Text.DeleteSelectedPreset;

    public Visibility SettingsVisibility => VisibilityStates.When(IsSettingsOpen);

    public Visibility SelectedSourceWarningVisibility =>
        MonitorEditorSurface.SelectedSourceWarningVisibility(SelectedSourceWarning);

    public Visibility ImageSourceEditorVisibility =>
        MonitorEditorSurface.ImageSourceEditorVisibility(EditSourceKind);

    public Visibility ColorSourceEditorVisibility =>
        MonitorEditorSurface.ColorSourceEditorVisibility(EditSourceKind);

    public string SelectedSourceWarning =>
        MonitorEditorSurface.SelectedSourceWarning(SelectedMonitor, Text);

    public ElementTheme RequestedTheme => ThemePreferenceMapper.ToElementTheme(SelectedThemePreference);

}
