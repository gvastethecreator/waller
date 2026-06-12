using Waller.Native.Core.Models;
using Windows.UI;

namespace Waller.Native.App.ViewModels;

public sealed partial class MainPageViewModel
{
    partial void OnSelectedMonitorChanged(MonitorRowViewModel? value)
    {
        var assignment = MonitorRowSelection.ApplySelection(Monitors, value);
        NotifySelectedMonitorSurfaceChanged();
        if (assignment is null)
        {
            return;
        }

        RefreshEditorFromAssignment(assignment);
    }

    partial void OnSelectedSourceOptionChanged(OptionItem<WallpaperSourceKind>? value)
    {
        if (value is not null)
        {
            EditSourceKind = value.Value;
        }
    }

    partial void OnSelectedFitOptionChanged(OptionItem<WallpaperFitMode>? value)
    {
        if (value is not null)
        {
            EditFitMode = value.Value;
        }
    }

    partial void OnSelectedAnchorOptionChanged(OptionItem<WallpaperAnchor>? value)
    {
        if (value is not null)
        {
            EditAnchor = value.Value;
        }
    }

    partial void OnEditSourceKindChanged(WallpaperSourceKind value)
    {
        SelectedSourceOption = OptionItems.Select(SourceOptions, value);
        RefreshSourceEditorVisibility();
        NotifyEditPermissionChanged();
        UpdateSelectedAssignment();
    }

    partial void OnEditImagePathChanged(string value) => UpdateSelectedAssignment();

    partial void OnEditColorHexChanged(string value)
    {
        if (isRefreshingColor)
        {
            return;
        }

        if (ColorHex.TryToColor(value, out var color))
        {
            isRefreshingColor = true;
            try
            {
                EditColor = color;
            }
            finally
            {
                isRefreshingColor = false;
            }
        }

        UpdateSelectedAssignment();
    }

    partial void OnEditColorChanged(Color value)
    {
        if (isRefreshingColor)
        {
            return;
        }

        isRefreshingColor = true;
        try
        {
            EditColorHex = ColorHex.FromColor(value);
        }
        finally
        {
            isRefreshingColor = false;
        }

        UpdateSelectedAssignment();
    }

    partial void OnEditFitModeChanged(WallpaperFitMode value)
    {
        SelectedFitOption = OptionItems.Select(FitOptions, value);
        UpdateSelectedAssignment();
    }

    partial void OnEditAnchorChanged(WallpaperAnchor value)
    {
        SelectedAnchorOption = OptionItems.Select(AnchorOptions, value);
        UpdateSelectedAssignment();
    }

    partial void OnEditOffsetXPercentChanged(double value) => UpdateSelectedAssignment();

    partial void OnEditOffsetYPercentChanged(double value) => UpdateSelectedAssignment();
}
