using Waller.Native.App.Platform;
using Waller.Native.Core.Models;
using Waller.Native.Core.Presets;

namespace Waller.Native.App.ViewModels;

internal sealed record ManagedPresetMutationResult<T>
{
    public ManagedPresetMutationResult(
        T? Value,
        bool Missing,
        bool WriteFailed)
    {
        if (Missing && WriteFailed)
        {
            throw new ArgumentException("Managed Preset mutation result cannot be both missing and write-failed.");
        }

        if (Missing || WriteFailed)
        {
            this.Value = default;
        }
        else
        {
            if (Value is null)
            {
                throw new ArgumentNullException(nameof(Value));
            }

            this.Value = Value;
        }

        this.Missing = Missing;
        this.WriteFailed = WriteFailed;
    }

    public T? Value { get; }

    public bool Missing { get; }

    public bool WriteFailed { get; }

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
        ArgumentNullException.ThrowIfNull(presetStore);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        return await TryMutationAsync(
            () => presetStore.RenameAsync(presetId, name),
            ManagedPresetMutationResult<Preset>.Success);
    }

    public static async Task<ManagedPresetMutationResult<Preset>> DuplicateAsync(
        PresetStore presetStore,
        Guid presetId,
        string nameDraft)
    {
        ArgumentNullException.ThrowIfNull(presetStore);
        ArgumentNullException.ThrowIfNull(nameDraft);

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
        ArgumentNullException.ThrowIfNull(presetStore);

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
        ArgumentNullException.ThrowIfNull(mutation);
        ArgumentNullException.ThrowIfNull(success);

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
