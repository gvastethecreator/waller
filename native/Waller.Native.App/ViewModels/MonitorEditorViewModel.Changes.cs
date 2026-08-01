using Waller.Native.Core.Models;
using Waller.Native.Workflows.MonitorEditing;
using Windows.UI;

namespace Waller.Native.App.ViewModels;

public sealed partial class MonitorEditorViewModel
{
    partial void OnSelectedMonitorChanged(MonitorRowViewModel? value)
    {
        MonitorRowSelection.ApplySelection(monitors, value);
        NotifySelectedMonitorSurfaceChanged();
        if (value is null)
        {
            return;
        }

        var result = workflow.Select(workspace.ActiveSession, value.MonitorKey);
        if (result.TryGetDraft(out var draft))
        {
            RefreshEditorFromDraft(draft);
        }
        else
        {
            setStatus(Text.NoMonitorSelected);
        }
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
        NotifySourceEditorVisibilityChanged();
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

    private void RefreshEditorFromDraft(MonitorEditorDraft draft)
    {
        ArgumentNullException.ThrowIfNull(draft);
        isRefreshingEditor = true;
        try
        {
            EditSourceKind = draft.SourceKind;
            EditImagePath = draft.ImagePath;
            EditColorHex = draft.ColorHex;
            EditColor = ColorHex.TryToColor(draft.ColorHex, out var color)
                ? color
                : Color.FromArgb(255, 0, 0, 0);
            EditFitMode = draft.FitMode;
            EditAnchor = draft.Anchor;
            SetEditOffsets(draft.OffsetXPercent, draft.OffsetYPercent, updateAssignment: false);
            RefreshSelectedEditorOptions();
            NotifySourceEditorVisibilityChanged();
        }
        finally
        {
            isRefreshingEditor = false;
        }
    }
}
