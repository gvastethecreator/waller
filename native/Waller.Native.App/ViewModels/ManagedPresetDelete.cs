using Waller.Native.Core.Models;
using Waller.Native.Core.Presets;

namespace Waller.Native.App.ViewModels;

internal sealed record ManagedPresetDeleteResult
{
    public ManagedPresetDeleteResult(
        bool Missing,
        bool WriteFailed,
        SelectedPresetSession? ReplacementSelection)
    {
        if (Missing && WriteFailed)
        {
            throw new ArgumentException("Managed Preset delete result cannot be both missing and write-failed.");
        }

        if ((Missing || WriteFailed) && ReplacementSelection is not null)
        {
            throw new ArgumentException("Failed Managed Preset delete results cannot include replacement selection.", nameof(ReplacementSelection));
        }

        this.Missing = Missing;
        this.WriteFailed = WriteFailed;
        this.ReplacementSelection = ReplacementSelection;
    }

    public bool Missing { get; }

    public bool WriteFailed { get; }

    public SelectedPresetSession? ReplacementSelection { get; }

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
        ArgumentNullException.ThrowIfNull(presetStore);
        ArgumentNullException.ThrowIfNull(activeSession);
        ArgumentNullException.ThrowIfNull(target);

        var deletedActivePreset = ActivePresetSession.IsBasedOn(activeSession, target.Id);
        var result = await ManagedPresetMutation.DeleteAsync(presetStore, target.Id);
        return new(
            result.Missing,
            result.WriteFailed,
            deletedActivePreset && result.TryGetValue(out _)
                ? SelectedPresetSessionFactory.DeletedActivePreset(activeSession)
                : null);
    }
}
