namespace Waller.Native.App.ViewModels;

internal sealed record ManagedPresetCommandInput(
    Guid Id,
    string NameDraft)
{
    public static bool TryRename(
        PresetMenuItem? selectedPreset,
        string nameDraft,
        PresetTextPresenter text,
        out ManagedPresetCommandInput input,
        out string statusText)
    {
        if (!TryGetId(selectedPreset, text.SelectToRename, out var id, out statusText))
        {
            input = Empty;
            return false;
        }

        if (!PresetNameInput.TryValidateRequired(nameDraft, text, out var name, out statusText))
        {
            input = Empty;
            return false;
        }

        input = new(id, name);
        statusText = string.Empty;
        return true;
    }

    public static bool TryDuplicate(
        PresetMenuItem? selectedPreset,
        string nameDraft,
        PresetTextPresenter text,
        out ManagedPresetCommandInput input,
        out string statusText)
    {
        if (!TryGetId(selectedPreset, text.SelectToDuplicate, out var id, out statusText))
        {
            input = Empty;
            return false;
        }

        input = new(id, nameDraft);
        statusText = string.Empty;
        return true;
    }

    public static bool TryDeleteConfirmation(
        PresetMenuItem? selectedPreset,
        PresetTextPresenter text,
        out PresetDeleteConfirmation confirmation,
        out string statusText)
    {
        if (!TryGetId(selectedPreset, text.SelectToDelete, out var id, out statusText))
        {
            confirmation = new PresetDeleteConfirmation(Guid.Empty, string.Empty);
            return false;
        }

        confirmation = ManagedPresetSelection.DeleteConfirmation(selectedPreset, id);
        statusText = string.Empty;
        return true;
    }

    private static ManagedPresetCommandInput Empty => new(Guid.Empty, string.Empty);

    private static bool TryGetId(
        PresetMenuItem? selectedPreset,
        string missingSelectionStatus,
        out Guid id,
        out string statusText)
    {
        if (ManagedPresetSelection.TryGetId(selectedPreset, out id))
        {
            statusText = string.Empty;
            return true;
        }

        statusText = missingSelectionStatus;
        return false;
    }
}
