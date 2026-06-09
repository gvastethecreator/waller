using Waller.Native.Core.Models;
using Waller.Native.Core.Presets;

namespace Waller.Native.App.ViewModels;

internal sealed record ManagedPresetDeleteResult(
    bool Missing,
    bool WriteFailed,
    SelectedPresetSession? ReplacementSelection)
{
    public bool DeletedActivePreset => ReplacementSelection is not null;

    public bool TryGetSuccessfulReplacement(out SelectedPresetSession? replacementSelection)
    {
        replacementSelection = ReplacementSelection;
        return !Missing && !WriteFailed;
    }
}

internal static class ManagedPresetDelete
{
    public static async Task<ManagedPresetDeleteResult> DeleteAsync(
        PresetStore presetStore,
        ActiveSession activeSession,
        PresetDeleteConfirmation target)
    {
        var deletedActivePreset = ActivePresetSession.IsBasedOn(activeSession, target.Id);
        var result = await ManagedPresetMutation.DeleteAsync(presetStore, target.Id);
        return new(
            result.Missing,
            result.WriteFailed,
            deletedActivePreset
                ? SelectedPresetSessionFactory.DeletedActivePreset(activeSession)
                : null);
    }
}
