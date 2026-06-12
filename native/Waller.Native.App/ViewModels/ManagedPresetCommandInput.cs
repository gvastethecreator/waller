using System.Diagnostics.CodeAnalysis;

namespace Waller.Native.App.ViewModels;

internal sealed record ManagedPresetCommandInput
{
    public ManagedPresetCommandInput(
        Guid Id,
        string NameDraft)
    {
        if (Id == Guid.Empty)
        {
            throw new ArgumentException("Managed Preset command id is required.", nameof(Id));
        }

        this.Id = Id;
        this.NameDraft = NameDraft ?? string.Empty;
    }

    public Guid Id { get; }

    public string NameDraft { get; }

    public static bool TryRename(
        PresetMenuItem? selectedPreset,
        string nameDraft,
        PresetTextPresenter text,
        [NotNullWhen(true)] out ManagedPresetCommandInput? input,
        out string statusText)
    {
        ArgumentNullException.ThrowIfNull(text);

        if (!TryGetId(selectedPreset, text.SelectToRename, out var id, out statusText))
        {
            input = null;
            return false;
        }

        if (!PresetNameInput.TryValidateRequired(nameDraft, text, out var name, out statusText))
        {
            input = null;
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
        [NotNullWhen(true)] out ManagedPresetCommandInput? input,
        out string statusText)
    {
        ArgumentNullException.ThrowIfNull(text);

        if (!TryGetId(selectedPreset, text.SelectToDuplicate, out var id, out statusText))
        {
            input = null;
            return false;
        }

        input = new(id, nameDraft);
        statusText = string.Empty;
        return true;
    }

    public static bool TryDeleteConfirmation(
        PresetMenuItem? selectedPreset,
        PresetTextPresenter text,
        [NotNullWhen(true)] out PresetDeleteConfirmation? confirmation,
        out string statusText)
    {
        ArgumentNullException.ThrowIfNull(text);

        if (!TryGetId(selectedPreset, text.SelectToDelete, out var id, out statusText))
        {
            confirmation = null;
            return false;
        }

        confirmation = ManagedPresetSelection.DeleteConfirmation(selectedPreset, id);
        statusText = string.Empty;
        return true;
    }

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
