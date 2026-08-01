using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using Waller.Native.App.Platform;
using Waller.Native.Core.Models;
using Waller.Native.Workflows.MonitorEditing;
using Waller.Native.Workflows.Shell;
using Windows.UI;

namespace Waller.Native.App.ViewModels;

public sealed partial class MonitorEditorViewModel : ObservableObject
{
    private readonly MonitorEditorWorkflow workflow;
    private readonly IImageFilePicker imageFilePicker;
    private readonly IShellWorkspace workspace;
    private readonly ObservableCollection<MonitorRowViewModel> monitors;
    private readonly Func<LocalizedText> text;
    private readonly Action<string> setStatus;
    private readonly Action<bool> refreshSessionSurface;
    private readonly MonitorEditTextPresenter editText;
    private bool isRefreshingEditor;
    private bool isRefreshingColor;

    public MonitorEditorViewModel(
        MonitorEditorWorkflow workflow,
        IImageFilePicker imageFilePicker,
        IShellWorkspace workspace,
        ObservableCollection<MonitorRowViewModel> monitors,
        Func<LocalizedText> text,
        Action<string> setStatus,
        Action<bool> refreshSessionSurface)
    {
        this.workflow = workflow ?? throw new ArgumentNullException(nameof(workflow));
        this.imageFilePicker = imageFilePicker ?? throw new ArgumentNullException(nameof(imageFilePicker));
        this.workspace = workspace ?? throw new ArgumentNullException(nameof(workspace));
        this.monitors = monitors ?? throw new ArgumentNullException(nameof(monitors));
        this.text = LocalizedTextSource.Require(text);
        this.setStatus = setStatus ?? throw new ArgumentNullException(nameof(setStatus));
        this.refreshSessionSurface = refreshSessionSurface ?? throw new ArgumentNullException(nameof(refreshSessionSurface));
        editText = new MonitorEditTextPresenter(this.text);
        RefreshEditorOptions();
    }

    public ObservableCollection<OptionItem<WallpaperSourceKind>> SourceOptions { get; } = [];

    public ObservableCollection<OptionItem<WallpaperFitMode>> FitOptions { get; } = [];

    public ObservableCollection<OptionItem<WallpaperAnchor>> AnchorOptions { get; } = [];

    public ObservableCollection<ColorSwatchOption> ColorSwatches { get; } =
        new(ColorSwatchCatalog.Defaults());

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

    public LocalizedText Text => text();

    public bool CanEditMonitorAssignment => workspace.CanEditMonitorAssignment;

    public bool CanEditPlacement => CanEditMonitorAssignment && EditSourceKind == WallpaperSourceKind.Image;

    public Microsoft.UI.Xaml.Visibility EditPanelVisibility =>
        MonitorEditorSurface.EditPanelVisibility(SelectedMonitor);

    public string SelectedMonitorDisplayName =>
        MonitorEditorSurface.SelectedMonitorDisplayName(SelectedMonitor, Text);

    public string SelectedMonitorResolution => SelectedMonitor?.Resolution ?? string.Empty;

    public int SelectedMonitorDisplayIndex =>
        SelectedMonitor?.Session.Monitor.Identity.DisplayIndex ?? 0;

    public Microsoft.UI.Xaml.Visibility SelectedSourceWarningVisibility =>
        MonitorEditorSurface.SelectedSourceWarningVisibility(SelectedSourceWarning);

    public Microsoft.UI.Xaml.Visibility ImageSourceEditorVisibility =>
        MonitorEditorSurface.ImageSourceEditorVisibility(EditSourceKind);

    public Microsoft.UI.Xaml.Visibility ColorSourceEditorVisibility =>
        MonitorEditorSurface.ColorSourceEditorVisibility(EditSourceKind);

    public string SelectedSourceWarning =>
        MonitorEditorSurface.SelectedSourceWarning(SelectedMonitor, Text);

    public void SelectProjectedMonitor(MonitorRowViewModel? monitor)
    {
        SelectedMonitor = monitor;
    }

    public void RefreshLocalizedSurface()
    {
        RefreshEditorOptions();
        NotifyPropertiesChanged(
            nameof(Text),
            nameof(SelectedMonitorDisplayName),
            nameof(SelectedMonitorResolution),
            nameof(SelectedSourceWarning),
            nameof(SelectedSourceWarningVisibility));
    }

    public void NotifyWorkspaceStateChanged()
    {
        NotifyPropertiesChanged(nameof(CanEditMonitorAssignment), nameof(CanEditPlacement));
    }

    private void NotifySelectedMonitorSurfaceChanged()
    {
        NotifyPropertiesChanged(
            nameof(EditPanelVisibility),
            nameof(SelectedMonitorDisplayName),
            nameof(SelectedMonitorResolution),
            nameof(SelectedMonitorDisplayIndex),
            nameof(SelectedSourceWarning),
            nameof(SelectedSourceWarningVisibility));
    }

    private void NotifySourceEditorVisibilityChanged()
    {
        NotifyPropertiesChanged(
            nameof(ImageSourceEditorVisibility),
            nameof(ColorSourceEditorVisibility),
            nameof(CanEditPlacement));
    }

    private void NotifyPropertiesChanged(params string[] propertyNames)
    {
        foreach (var propertyName in ViewModelNotificationGroups.Require(propertyNames))
        {
            OnPropertyChanged(propertyName);
        }
    }
}
