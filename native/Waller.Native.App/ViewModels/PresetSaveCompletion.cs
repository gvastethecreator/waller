using Waller.Native.Core.Models;

namespace Waller.Native.App.ViewModels;

internal sealed record PresetSaveCompletion(
    Preset SelectedPresetRecord,
    string? PresetNameDraft)
{
    public static PresetSaveCompletion Existing(Preset preset) =>
        new(preset, PresetNameDraft: null);

    public static PresetSaveCompletion New(Preset preset) =>
        new(preset, preset.Name);
}
