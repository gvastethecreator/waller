using Waller.Native.Core.Models;

namespace Waller.Native.App.ViewModels;

internal sealed record PresetSaveCompletion
{
    public PresetSaveCompletion(
        Preset SelectedPresetRecord,
        string? PresetNameDraft)
    {
        ArgumentNullException.ThrowIfNull(SelectedPresetRecord);
        if (PresetNameDraft is not null)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(PresetNameDraft);
        }

        this.SelectedPresetRecord = SelectedPresetRecord;
        this.PresetNameDraft = PresetNameDraft;
    }

    public Preset SelectedPresetRecord { get; }

    public string? PresetNameDraft { get; }

    public static PresetSaveCompletion Existing(Preset preset) =>
        new(preset, PresetNameDraft: null);

    public static PresetSaveCompletion New(Preset preset) =>
        new(preset, preset.Name);
}
