using Waller.Native.Core.Models;
using Waller.Native.Core.Presets;

namespace Waller.Native.App.ViewModels;

internal sealed record PresetSaveCompletion
{
    public PresetSaveCompletion(
        Preset SelectedPresetRecord,
        string? PresetNameDraft)
    {
        ArgumentNullException.ThrowIfNull(SelectedPresetRecord);

        this.SelectedPresetRecord = SelectedPresetRecord;
        this.PresetNameDraft = PresetNameDraft is null
            ? null
            : PresetNames.Validate(PresetNameDraft, nameof(PresetNameDraft));
    }

    public Preset SelectedPresetRecord { get; }

    public string? PresetNameDraft { get; }

    public static PresetSaveCompletion Existing(Preset preset) =>
        new(preset, PresetNameDraft: null);

    public static PresetSaveCompletion New(Preset preset) =>
        new(preset, preset.Name);
}
