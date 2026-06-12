using CommunityToolkit.Mvvm.Input;

namespace Waller.Native.App.ViewModels;

public sealed partial class MainPageViewModel
{
    [RelayCommand]
    private void ResetPosition()
    {
        if (!CanEditPlacement)
        {
            return;
        }

        SetEditOffsets(0, 0, updateAssignment: true);
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
}
