using Waller.Native.App.Platform;
using Waller.Native.Core.Models;
using Waller.Native.Core.Presets;

namespace Waller.Native.App.ViewModels;

internal sealed record PresetSessionSaveResult(
    Preset? Preset,
    bool WriteFailed)
{
    public static PresetSessionSaveResult Success(Preset preset) => new(preset, WriteFailed: false);

    public static PresetSessionSaveResult LocalWriteFailed() => new(Preset: null, WriteFailed: true);

    public bool TryGetPreset(out Preset preset)
    {
        if (!WriteFailed && Preset is { } savedPreset)
        {
            preset = savedPreset;
            return true;
        }

        preset = null!;
        return false;
    }
}

internal static class PresetSessionSave
{
    public static async Task<PresetSessionSaveResult> SaveExistingAsync(
        PresetStore presetStore,
        ActiveSession session,
        Preset selectedPresetRecord)
    {
        var preset = PresetFactory.UpdateFromSession(
            session,
            selectedPresetRecord.Identity,
            selectedPresetRecord.CreatedAt);

        return await SaveAsync(presetStore, preset);
    }

    public static async Task<PresetSessionSaveResult> SaveAsAsync(
        PresetStore presetStore,
        ActiveSession session,
        string name)
    {
        return await SaveAsync(
            presetStore,
            PresetFactory.CreateFromSession(session, name));
    }

    private static async Task<PresetSessionSaveResult> SaveAsync(
        PresetStore presetStore,
        Preset preset)
    {
        return await LocalDataWriteGuard.TryAsync(
            async () => PresetSessionSaveResult.Success(await presetStore.SaveAsync(preset)),
            PresetSessionSaveResult.LocalWriteFailed());
    }
}
