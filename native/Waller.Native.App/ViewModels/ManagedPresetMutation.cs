using Waller.Native.App.Platform;
using Waller.Native.Core.Models;
using Waller.Native.Core.Presets;

namespace Waller.Native.App.ViewModels;

internal sealed record ManagedPresetMutationResult<T>(
    T? Value,
    bool Missing,
    bool WriteFailed)
{
    public static ManagedPresetMutationResult<T> Success(T value) => new(value, Missing: false, WriteFailed: false);

    public static ManagedPresetMutationResult<T> MissingPreset() => new(default, Missing: true, WriteFailed: false);

    public static ManagedPresetMutationResult<T> LocalWriteFailed() => new(default, Missing: false, WriteFailed: true);

    public bool TryGetValue(out T value)
    {
        if (!Missing && !WriteFailed && Value is { } successValue)
        {
            value = successValue;
            return true;
        }

        value = default!;
        return false;
    }
}

internal static class ManagedPresetMutation
{
    public static async Task<ManagedPresetMutationResult<Preset>> RenameAsync(
        PresetStore presetStore,
        Guid presetId,
        string name)
    {
        return await TryMutationAsync(
            () => presetStore.RenameAsync(presetId, name),
            ManagedPresetMutationResult<Preset>.Success);
    }

    public static async Task<ManagedPresetMutationResult<Preset>> DuplicateAsync(
        PresetStore presetStore,
        Guid presetId,
        string nameDraft)
    {
        return await TryMutationAsync(
            async () =>
            {
                var preset = await presetStore.LoadAsync(presetId)
                    ?? throw new FileNotFoundException($"Preset not found: {presetId}");
                var name = PresetNames.DuplicateName(preset.Name, nameDraft);
                return await presetStore.DuplicateAsync(preset, name);
            },
            ManagedPresetMutationResult<Preset>.Success);
    }

    public static async Task<ManagedPresetMutationResult<bool>> DeleteAsync(
        PresetStore presetStore,
        Guid presetId)
    {
        return await TryMutationAsync(
            async () =>
            {
                await presetStore.DeleteAsync(presetId);
                return true;
            },
            ManagedPresetMutationResult<bool>.Success);
    }

    private static async Task<ManagedPresetMutationResult<T>> TryMutationAsync<T>(
        Func<Task<T>> mutation,
        Func<T, ManagedPresetMutationResult<T>> success)
    {
        try
        {
            return success(await mutation());
        }
        catch (FileNotFoundException)
        {
            return ManagedPresetMutationResult<T>.MissingPreset();
        }
        catch (Exception error) when (LocalDataWriteGuard.IsRecoverable(error))
        {
            return ManagedPresetMutationResult<T>.LocalWriteFailed();
        }
    }
}
