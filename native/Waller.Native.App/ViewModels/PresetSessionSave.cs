using Waller.Native.App.Platform;
using Waller.Native.Core.Models;
using Waller.Native.Core.Presets;

namespace Waller.Native.App.ViewModels;

internal sealed record PresetSessionSaveResult
{
    public PresetSessionSaveResult(Preset? Preset, bool WriteFailed)
    {
        this.Preset = WriteFailed
            ? null
            : Preset ?? throw new ArgumentNullException(nameof(Preset));
        this.WriteFailed = WriteFailed;
    }

    public Preset? Preset { get; }

    public bool WriteFailed { get; }

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
        ArgumentNullException.ThrowIfNull(presetStore);
        ArgumentNullException.ThrowIfNull(session);
        ArgumentNullException.ThrowIfNull(selectedPresetRecord);

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
        ArgumentNullException.ThrowIfNull(presetStore);
        ArgumentNullException.ThrowIfNull(session);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        return await SaveAsync(
            presetStore,
            PresetFactory.CreateFromSession(session, name));
    }

    private static async Task<PresetSessionSaveResult> SaveAsync(
        PresetStore presetStore,
        Preset preset)
    {
        ArgumentNullException.ThrowIfNull(presetStore);
        ArgumentNullException.ThrowIfNull(preset);

        return await LocalDataWriteGuard.TryAsync(
            async () => PresetSessionSaveResult.Success(await presetStore.SaveAsync(preset)),
            PresetSessionSaveResult.LocalWriteFailed());
    }
}
