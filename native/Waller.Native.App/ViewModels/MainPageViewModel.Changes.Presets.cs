namespace Waller.Native.App.ViewModels;

public sealed partial class MainPageViewModel
{
    partial void OnSelectedPresetChanged(PresetMenuItem? value)
    {
        NotifySessionSummaryChanged();
        if (isChangingPresetSelection || value is null)
        {
            return;
        }

        var loadVersion = ++selectedPresetLoadVersion;
        _ = LoadSelectedPresetAsync(value, loadVersion);
    }

    partial void OnSelectedManagePresetChanged(PresetMenuItem? value)
    {
        ManagePresetNameDraft = ManagedPresetSelection.NameDraft(value);
        ClearPendingDeletePreset();
    }
}
