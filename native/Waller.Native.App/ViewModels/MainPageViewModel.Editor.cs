using CommunityToolkit.Mvvm.Input;
using Waller.Native.Core.Models;

namespace Waller.Native.App.ViewModels;

public sealed partial class MainPageViewModel
{
    [RelayCommand]
    private async Task ChooseImage()
    {
        if (!CanUseShellCommands)
        {
            return;
        }

        var result = MonitorSourceSelectionFactory.FromPickedImage(
            await imageFilePicker.PickImagePathAsync(),
            monitorEditText);
        if (result.Selection is not null)
        {
            ApplySourceSelection(result.Selection);
        }

        StatusText = result.StatusText;
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
    private void SelectMonitor(MonitorRowViewModel? monitor)
    {
        if (monitor is not null)
        {
            SelectedMonitor = monitor;
        }
    }

    [RelayCommand]
    private void ForgetMissingMonitor(MissingMonitorRowViewModel? monitor)
    {
        if (!CanEditMonitorAssignment || monitor is null)
        {
            return;
        }

        ApplyDisconnectedMonitorEdit(
            DisconnectedMonitorEdit.Forget(sessionEditor, activeSession, monitor, monitorEditText));
    }

    [RelayCommand]
    private void ReassignMissingMonitor(MissingMonitorRowViewModel? monitor)
    {
        if (!CanEditMonitorAssignment || monitor is null)
        {
            return;
        }

        ApplyDisconnectedMonitorEdit(
            DisconnectedMonitorEdit.Reassign(sessionEditor, activeSession, monitor, SelectedMonitor, monitorEditText));
    }

    [RelayCommand]
    private void ResetPosition()
    {
        if (!CanEditPlacement)
        {
            return;
        }

        SetEditOffsets(0, 0, updateAssignment: true);
    }

    private void ApplyDisconnectedMonitorEdit(DisconnectedMonitorEditResult result)
    {
        if (result.Session is not null)
        {
            activeSession = result.Session;
            RefreshSessionSurface(selectFirst: false);
        }

        StatusText = result.StatusText;
    }

    private void UpdateSelectedAssignment()
    {
        if (isRefreshingEditor || !CanEditMonitorAssignment || SelectedMonitor is null || Monitors.Count == 0)
        {
            return;
        }

        var result = MonitorAssignmentUpdate.ApplyFromEditorFields(
            sessionEditor,
            activeSession,
            SelectedMonitor.MonitorKey,
            EditSourceKind,
            EditImagePath,
            EditColorHex,
            EditColor,
            EditFitMode,
            EditAnchor,
            EditOffsetXPercent,
            EditOffsetYPercent);
        StatusText = result.StatusText(monitorEditText, SelectedMonitor.DisplayName);
        if (!result.TryGetUpdatedSession(out var updatedSession))
        {
            return;
        }

        activeSession = updatedSession;
        RefreshSessionSurface(selectFirst: false);
        NotifySelectedSourceWarningChanged();
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

    private void RefreshEditorFromAssignment(PresetAssignment assignment)
    {
        isRefreshingEditor = true;
        try
        {
            var draft = MonitorEditDraft.FromAssignment(assignment);
            EditSourceKind = draft.SourceKind;
            EditImagePath = draft.ImagePath;
            EditColorHex = draft.ColorHex;
            EditColor = draft.Color;
            EditFitMode = draft.FitMode;
            EditAnchor = draft.Anchor;
            SetEditOffsets(draft.OffsetXPercent, draft.OffsetYPercent, updateAssignment: false);
            RefreshSelectedEditorOptions();
            RefreshSourceEditorVisibility();
        }
        finally
        {
            isRefreshingEditor = false;
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

    private void RefreshSourceEditorVisibility()
    {
        NotifyPropertiesChanged(ViewModelNotificationGroups.SourceEditorVisibility);
    }

    private void RefreshEditorOptions()
    {
        var selection = LocalizedOptionSelections.RefreshEditor(
            SourceOptions,
            FitOptions,
            AnchorOptions,
            Text,
            EditSourceKind,
            EditFitMode,
            EditAnchor);
        ApplyEditorOptionSelection(selection);
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
