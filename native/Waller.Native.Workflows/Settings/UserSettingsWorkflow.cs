using System.Text.Json;
using Waller.Native.Core.Settings;
using Waller.Native.Core.Storage;

namespace Waller.Native.Workflows.Settings;

public sealed class UserSettingsWorkflow
{
    private readonly object queueGate = new();
    private readonly UserSettingsStore store;
    private Task queueTail = Task.CompletedTask;

    public UserSettingsWorkflow(UserSettingsStore store)
    {
        ArgumentNullException.ThrowIfNull(store);
        this.store = store;
    }

    public Task<UserSettings> LoadAsync(CancellationToken cancellationToken = default) =>
        Enqueue(token => store.LoadAsync(token), cancellationToken);

    public Task<UserSettingsUpdateResult> UpdatePreferencesAsync(
        AppThemePreference theme,
        string language,
        Guid? lastSelectedPresetId,
        CancellationToken cancellationToken = default) =>
        EnqueueUpdate(
            current => current.WithPreferences(theme, language, lastSelectedPresetId),
            cancellationToken);

    public Task<UserSettingsUpdateResult> UpdateLastSelectedPresetAsync(
        Guid? lastSelectedPresetId,
        CancellationToken cancellationToken = default) =>
        EnqueueUpdate(
            current => current.WithLastSelectedPreset(lastSelectedPresetId),
            cancellationToken);

    public Task<UserSettingsUpdateResult> UpdateWindowPlacementAsync(
        int width,
        int height,
        int x,
        int y,
        CancellationToken cancellationToken = default) =>
        EnqueueUpdate(
            current => current.WithWindowPlacement(width, height, x, y),
            cancellationToken);

    private Task<UserSettingsUpdateResult> EnqueueUpdate(
        Func<UserSettings, UserSettings> update,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(update);
        return Enqueue(
            token => UpdateAsync(update, token),
            cancellationToken);
    }

    private async Task<UserSettingsUpdateResult> UpdateAsync(
        Func<UserSettings, UserSettings> update,
        CancellationToken cancellationToken)
    {
        try
        {
            var current = await store.LoadForUpdateAsync(cancellationToken).ConfigureAwait(false);
            var updated = update(current);
            await store.SaveAsync(updated, cancellationToken).ConfigureAwait(false);
            return UserSettingsUpdateResult.Saved(updated);
        }
        catch (Exception error) when (IsRecoverable(error))
        {
            return UserSettingsUpdateResult.RecoverableFailure();
        }
    }

    private Task<T> Enqueue<T>(
        Func<CancellationToken, Task<T>> operation,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(operation);

        lock (queueGate)
        {
            var queued = RunAfterAsync(queueTail, operation, cancellationToken);
            queueTail = ContinueQueueAsync(queued);
            return queued;
        }
    }

    private static async Task<T> RunAfterAsync<T>(
        Task predecessor,
        Func<CancellationToken, Task<T>> operation,
        CancellationToken cancellationToken)
    {
        await predecessor.ConfigureAwait(false);
        cancellationToken.ThrowIfCancellationRequested();
        return await operation(cancellationToken).ConfigureAwait(false);
    }

    private static async Task ContinueQueueAsync(Task operation)
    {
        try
        {
            await operation.ConfigureAwait(false);
        }
        catch
        {
            // Each caller observes its own failure. The queue must remain usable.
        }
    }

    private static bool IsRecoverable(Exception error) =>
        error is JsonException or NotSupportedException
        || LocalDataFileSystemErrors.IsRecoverable(error);
}
