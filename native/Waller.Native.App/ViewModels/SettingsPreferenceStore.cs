using Waller.Native.App.Platform;
using Waller.Native.Core.Models;
using Waller.Native.Core.Settings;

namespace Waller.Native.App.ViewModels;

internal sealed record SettingsPreferenceSaveResult
{
    public SettingsPreferenceSaveResult(
        Guid? LastSelectedPresetId,
        bool WriteFailed)
    {
        var normalizedLastSelectedPresetId = PresetIds.NormalizeOptional(LastSelectedPresetId);
        if (WriteFailed && normalizedLastSelectedPresetId is not null)
        {
            throw new ArgumentException("Failed Settings save results cannot include last selected Preset.", nameof(LastSelectedPresetId));
        }

        this.LastSelectedPresetId = normalizedLastSelectedPresetId;
        this.WriteFailed = WriteFailed;
    }

    public Guid? LastSelectedPresetId { get; }

    public bool WriteFailed { get; }

    public static SettingsPreferenceSaveResult Success(Guid? lastSelectedPresetId) =>
        new(lastSelectedPresetId, WriteFailed: false);

    public static SettingsPreferenceSaveResult LocalWriteFailed() =>
        new(LastSelectedPresetId: null, WriteFailed: true);

    public string StatusText(ShellStatusTextPresenter shellText)
    {
        ArgumentNullException.ThrowIfNull(shellText);

        return WriteFailed
            ? shellText.LocalDataWriteFailed
            : shellText.SettingsSaved;
    }

    public bool TryGetSavedLastSelectedPresetId(out Guid? lastSelectedPresetId)
    {
        lastSelectedPresetId = LastSelectedPresetId;
        return !WriteFailed;
    }
}

internal static class SettingsPreferenceStore
{
    public static async Task<SettingsPreferenceDraft> LoadDraftAsync(UserSettingsStore settingsStore)
    {
        ArgumentNullException.ThrowIfNull(settingsStore);

        return SettingsPreferenceDraft.From(await settingsStore.LoadAsync());
    }

    public static async Task<SettingsPreferenceSaveResult> SaveRequestAsync(
        UserSettingsStore settingsStore,
        SettingsSaveRequest request)
    {
        ArgumentNullException.ThrowIfNull(settingsStore);
        ArgumentNullException.ThrowIfNull(request);

        return await LocalDataWriteGuard.TryAsync(
            async () =>
            {
                var settings = await settingsStore.LoadAsync();
                await settingsStore.SaveAsync(request.ApplyTo(settings));
                return SettingsPreferenceSaveResult.Success(request.LastSelectedPresetId);
            },
            SettingsPreferenceSaveResult.LocalWriteFailed());
    }

    public static async Task<Guid?> PersistLastSelectedPresetAsync(
        UserSettingsStore settingsStore,
        Guid? presetId)
    {
        ArgumentNullException.ThrowIfNull(settingsStore);
        var normalizedPresetId = PresetIds.NormalizeOptional(presetId);

        return await LocalDataWriteGuard.TryAsync<Guid?>(
            async () =>
            {
                var settings = await settingsStore.LoadAsync();
                await settingsStore.SaveAsync(settings.WithLastSelectedPreset(normalizedPresetId));
                return normalizedPresetId;
            },
            fallback: null);
    }
}
