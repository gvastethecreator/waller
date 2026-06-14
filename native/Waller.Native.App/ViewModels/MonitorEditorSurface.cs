using Microsoft.UI.Xaml;
using Waller.Native.Core.Models;

namespace Waller.Native.App.ViewModels;

internal static class MonitorEditorSurface
{
    public static Visibility EditPanelVisibility(MonitorRowViewModel? selectedMonitor) =>
        VisibilityStates.When(selectedMonitor is not null);

    public static string SelectedMonitorDisplayName(MonitorRowViewModel? selectedMonitor, LocalizedText text)
    {
        ArgumentNullException.ThrowIfNull(text);

        return selectedMonitor?.DisplayName ?? text.NoMonitorSelected;
    }

    public static Visibility ImageSourceEditorVisibility(WallpaperSourceKind sourceKind)
    {
        DefinedEnumValue.Require(
            sourceKind,
            nameof(sourceKind),
            "Unknown editor source kind.");

        return VisibilityStates.When(sourceKind == WallpaperSourceKind.Image);
    }

    public static Visibility ColorSourceEditorVisibility(WallpaperSourceKind sourceKind)
    {
        DefinedEnumValue.Require(
            sourceKind,
            nameof(sourceKind),
            "Unknown editor source kind.");

        return VisibilityStates.When(sourceKind == WallpaperSourceKind.SolidColor);
    }

    public static string SelectedSourceWarning(MonitorRowViewModel? selectedMonitor, LocalizedText text)
    {
        ArgumentNullException.ThrowIfNull(text);

        if (selectedMonitor?.Session.DesiredAssignment.Source is not { } source)
        {
            return string.Empty;
        }

        DefinedEnumValue.Require(
            source.Kind,
            nameof(source.Kind),
            "Unknown editor source kind.");

        if (source.Kind != WallpaperSourceKind.Image)
        {
            return string.Empty;
        }

        return text.SelectedSourceWarning(source);
    }

    public static Visibility SelectedSourceWarningVisibility(string selectedSourceWarning) =>
        VisibilityStates.Unless(string.IsNullOrWhiteSpace(selectedSourceWarning));
}
