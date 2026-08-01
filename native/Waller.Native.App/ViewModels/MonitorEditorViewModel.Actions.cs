using CommunityToolkit.Mvvm.Input;
using Waller.Native.Core.Models;
using Waller.Native.Workflows.MonitorEditing;

namespace Waller.Native.App.ViewModels;

public sealed partial class MonitorEditorViewModel
{
    [RelayCommand]
    private void SelectMonitor(MonitorRowViewModel? monitor)
    {
        if (monitor is not null)
        {
            SelectedMonitor = monitor;
        }
    }

    [RelayCommand]
    private async Task ChooseImage()
    {
        if (!workspace.CanUseShellCommands)
        {
            return;
        }

        var result = MonitorSourceSelectionFactory.FromPickedImage(
            await imageFilePicker.PickImagePathAsync(),
            editText);
        if (result.Selection is not null)
        {
            ApplySourceSelection(result.Selection);
        }

        setStatus(result.StatusText);
    }

    [RelayCommand]
    private void SelectColorSwatch(ColorSwatchOption? swatch)
    {
        if (!CanEditMonitorAssignment || swatch is null)
        {
            return;
        }

        ApplySourceSelection(MonitorSourceSelectionFactory.FromSwatch(swatch));
    }

    [RelayCommand]
    private void ResetPosition()
    {
        if (CanEditPlacement)
        {
            SetEditOffsets(0, 0, updateAssignment: true);
        }
    }

    [RelayCommand]
    private void ForgetMissingMonitor(MissingMonitorRowViewModel? monitor)
    {
        if (!CanEditMonitorAssignment || monitor is null)
        {
            return;
        }

        var result = workflow.ForgetDisconnected(
            workspace.ActiveSession,
            monitor.Assignment.SavedMonitor.MonitorKey);
        ApplyDisconnectedOutcome(result, editText.ForgotDisconnectedMonitor(monitor.DisplayName));
    }

    [RelayCommand]
    private void ReassignMissingMonitor(MissingMonitorRowViewModel? monitor)
    {
        if (!CanEditMonitorAssignment || monitor is null)
        {
            return;
        }

        if (SelectedMonitor is null)
        {
            setStatus(editText.SelectMonitorBeforeReassign);
            return;
        }

        var result = workflow.ReassignDisconnected(
            workspace.ActiveSession,
            monitor.Assignment.SavedMonitor.MonitorKey,
            SelectedMonitor.MonitorKey);
        ApplyDisconnectedOutcome(
            result,
            editText.ReassignedDisconnectedMonitor(monitor.DisplayName, SelectedMonitor.DisplayName));
    }

    private void UpdateSelectedAssignment()
    {
        if (isRefreshingEditor || !CanEditMonitorAssignment || SelectedMonitor is null || monitors.Count == 0)
        {
            return;
        }

        var result = workflow.Update(
            workspace.ActiveSession,
            SelectedMonitor.MonitorKey,
            new MonitorEditorDraft(
                EditSourceKind,
                EditImagePath,
                EditColorHex,
                EditFitMode,
                EditAnchor,
                EditOffsetXPercent,
                EditOffsetYPercent));

        ApplyUpdateOutcome(result, SelectedMonitor.DisplayName);
    }

    private void ApplyUpdateOutcome(MonitorEditorResult result, string monitorName)
    {
        ArgumentNullException.ThrowIfNull(result);
        setStatus(result.Status switch
        {
            MonitorEditorStatus.Updated or MonitorEditorStatus.Unchanged => editText.PendingChanges(monitorName),
            MonitorEditorStatus.ImageMissing when string.IsNullOrWhiteSpace(result.MissingImagePath) =>
                editText.ImagePathRequired,
            MonitorEditorStatus.ImageMissing => editText.MissingImage(result.MissingImagePath!),
            MonitorEditorStatus.InvalidValue => editText.InvalidEditValue(result.ValidationError!),
            MonitorEditorStatus.MonitorMissing => Text.NoMonitorSelected,
            _ => Text.CheckValue,
        });
        ReplaceUpdatedSession(result);
    }

    private void ApplyDisconnectedOutcome(MonitorEditorResult result, string successStatus)
    {
        ArgumentNullException.ThrowIfNull(result);
        setStatus(result.Status == MonitorEditorStatus.Updated
            ? successStatus
            : result.Status == MonitorEditorStatus.TargetMonitorMissing
                ? editText.SelectMonitorBeforeReassign
                : Text.CheckValue);
        ReplaceUpdatedSession(result);
    }

    private void ReplaceUpdatedSession(MonitorEditorResult result)
    {
        if (!result.TryGetUpdatedSession(out var updatedSession))
        {
            return;
        }

        workspace.ReplaceActiveSession(updatedSession);
        refreshSessionSurface(false);
    }

    private void ApplySourceSelection(MonitorSourceSelection selection)
    {
        EditSourceKind = selection.SourceKind;
        if (selection.ImagePath is not null)
        {
            EditImagePath = selection.ImagePath;
        }

        if (selection.ColorHex is not null)
        {
            EditColorHex = selection.ColorHex;
        }
    }

    private void SetEditOffsets(double offsetXPercent, double offsetYPercent, bool updateAssignment)
    {
        var wasRefreshingEditor = isRefreshingEditor;
        isRefreshingEditor = true;
        try
        {
            EditOffsetXPercent = offsetXPercent;
            EditOffsetYPercent = offsetYPercent;
        }
        finally
        {
            isRefreshingEditor = wasRefreshingEditor;
        }

        if (updateAssignment)
        {
            UpdateSelectedAssignment();
        }
    }

    private void RefreshEditorOptions()
    {
        ApplyEditorOptionSelection(LocalizedOptionSelections.RefreshEditor(
            SourceOptions,
            FitOptions,
            AnchorOptions,
            Text,
            EditSourceKind,
            EditFitMode,
            EditAnchor));
    }

    private void RefreshSelectedEditorOptions()
    {
        ApplyEditorOptionSelection(LocalizedOptionSelections.SelectEditor(
            SourceOptions,
            FitOptions,
            AnchorOptions,
            EditSourceKind,
            EditFitMode,
            EditAnchor));
    }

    private void ApplyEditorOptionSelection(EditorOptionSelection selection)
    {
        SelectedSourceOption = selection.Source;
        SelectedFitOption = selection.Fit;
        SelectedAnchorOption = selection.Anchor;
    }
}
