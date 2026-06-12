using CommunityToolkit.Mvvm.Input;
using Waller.Native.Core.Models;

namespace Waller.Native.App.ViewModels;

public sealed partial class MainPageViewModel
{
    [RelayCommand]
    private void SelectMonitor(MonitorRowViewModel? monitor)
    {
        if (monitor is not null)
        {
            SelectedMonitor = monitor;
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
}
