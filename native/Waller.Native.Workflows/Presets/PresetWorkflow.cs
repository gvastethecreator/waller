using Waller.Native.Core.Models;
using Waller.Native.Core.Presets;
using Waller.Native.Core.Storage;

namespace Waller.Native.Workflows.Presets;

public sealed class PresetWorkflow
{
    private readonly PresetMatcher matcher = new();
    private readonly PresetStore store;

    public PresetWorkflow(PresetStore store)
    {
        ArgumentNullException.ThrowIfNull(store);
        this.store = store;
    }

    public Task<IReadOnlyList<Preset>> ListAsync(CancellationToken cancellationToken = default) =>
        store.ListAsync(cancellationToken);

    public async Task<PresetOperationResult<PresetSelection>> SelectAsync(
        ActiveSession session,
        Guid? presetId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(session);
        var selectedId = PresetIds.NormalizeOptional(presetId);
        if (selectedId is null)
        {
            return PresetOperationResult<PresetSelection>.Success(
                new PresetSelection(
                    session with
                    {
                        BasedOnPreset = null,
                        HasUnsavedPresetChanges = false,
                        MissingAssignments = [],
                    },
                    selectedPreset: null));
        }

        var preset = await store.LoadAsync(selectedId.Value, cancellationToken).ConfigureAwait(false);
        return preset is null
            ? PresetOperationResult<PresetSelection>.Missing()
            : PresetOperationResult<PresetSelection>.Success(
                new PresetSelection(matcher.ApplyPreset(session, preset), preset));
    }

    public Task<PresetOperationResult<Preset>> SaveExistingAsync(
        ActiveSession session,
        Preset selectedPreset,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(session);
        ArgumentNullException.ThrowIfNull(selectedPreset);
        var updated = PresetFactory.UpdateFromSession(
            session,
            selectedPreset.Identity,
            selectedPreset.CreatedAt);
        return SaveAsync(updated, cancellationToken);
    }

    public Task<PresetOperationResult<Preset>> SaveAsAsync(
        ActiveSession session,
        string name,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(session);
        var preset = PresetFactory.CreateFromSession(
            session,
            PresetNames.Validate(name, nameof(name)));
        return SaveAsync(preset, cancellationToken);
    }

    public async Task<PresetOperationResult<Preset>> RenameAsync(
        Guid presetId,
        string name,
        CancellationToken cancellationToken = default)
    {
        var validId = PresetIds.RequireValid(presetId, nameof(presetId));
        var validName = PresetNames.Validate(name, nameof(name));
        var preset = await store.LoadAsync(validId, cancellationToken).ConfigureAwait(false);
        return preset is null
            ? PresetOperationResult<Preset>.Missing()
            : await SaveAsync(PresetFactory.Rename(preset, validName), cancellationToken).ConfigureAwait(false);
    }

    public async Task<PresetOperationResult<Preset>> DuplicateAsync(
        Guid presetId,
        string? requestedName,
        CancellationToken cancellationToken = default)
    {
        var validId = PresetIds.RequireValid(presetId, nameof(presetId));
        var preset = await store.LoadAsync(validId, cancellationToken).ConfigureAwait(false);
        if (preset is null)
        {
            return PresetOperationResult<Preset>.Missing();
        }

        var duplicateName = PresetNames.DuplicateName(preset.Name, requestedName);
        return await SaveAsync(
            PresetFactory.Duplicate(preset, duplicateName),
            cancellationToken).ConfigureAwait(false);
    }

    public async Task<PresetOperationResult<PresetDeletion>> DeleteAsync(
        ActiveSession session,
        Guid presetId,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(session);
        var validId = PresetIds.RequireValid(presetId, nameof(presetId));
        var preset = await store.LoadAsync(validId, cancellationToken).ConfigureAwait(false);
        if (preset is null)
        {
            return PresetOperationResult<PresetDeletion>.Missing();
        }

        try
        {
            await store.DeleteAsync(validId, cancellationToken).ConfigureAwait(false);
            var deletedActivePreset = session.BasedOnPreset?.Id == validId;
            var nextSession = deletedActivePreset
                ? session with
                {
                    BasedOnPreset = null,
                    HasUnsavedPresetChanges = true,
                }
                : session;
            return PresetOperationResult<PresetDeletion>.Success(
                new PresetDeletion(nextSession, deletedActivePreset));
        }
        catch (Exception error) when (LocalDataFileSystemErrors.IsRecoverable(error))
        {
            return PresetOperationResult<PresetDeletion>.WriteFailed();
        }
    }

    private async Task<PresetOperationResult<Preset>> SaveAsync(
        Preset preset,
        CancellationToken cancellationToken)
    {
        try
        {
            return PresetOperationResult<Preset>.Success(
                await store.SaveAsync(preset, cancellationToken).ConfigureAwait(false));
        }
        catch (Exception error) when (LocalDataFileSystemErrors.IsRecoverable(error))
        {
            return PresetOperationResult<Preset>.WriteFailed();
        }
    }
}
