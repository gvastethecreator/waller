using CommunityToolkit.Mvvm.Input;

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

    private void RefreshSourceEditorVisibility()
    {
        NotifyPropertiesChanged(ViewModelNotificationGroups.SourceEditorVisibility);
    }
}
