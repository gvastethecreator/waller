using Microsoft.UI.Xaml;
using Waller.Native.Core.Models;

namespace Waller.Native.App.ViewModels;

public sealed partial class MainPageViewModel
{
    public Visibility EditPanelVisibility =>
        MonitorEditorSurface.EditPanelVisibility(SelectedMonitor);

    public bool CanEditMonitorAssignment => InteractionState.CanEditMonitorAssignment;

    public bool CanEditPlacement => CanEditMonitorAssignment && EditSourceKind == WallpaperSourceKind.Image;

    public string SelectedMonitorDisplayName =>
        MonitorEditorSurface.SelectedMonitorDisplayName(SelectedMonitor, Text);

    public string SelectedMonitorResolution => SelectedMonitor?.Resolution ?? string.Empty;

    public int SelectedMonitorDisplayIndex =>
        SelectedMonitor?.Session.Monitor.Identity.DisplayIndex ?? 0;

    public Visibility SelectedSourceWarningVisibility =>
        MonitorEditorSurface.SelectedSourceWarningVisibility(SelectedSourceWarning);

    public Visibility ImageSourceEditorVisibility =>
        MonitorEditorSurface.ImageSourceEditorVisibility(EditSourceKind);

    public Visibility ColorSourceEditorVisibility =>
        MonitorEditorSurface.ColorSourceEditorVisibility(EditSourceKind);

    public string SelectedSourceWarning =>
        MonitorEditorSurface.SelectedSourceWarning(SelectedMonitor, Text);
}
