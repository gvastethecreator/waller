using Waller.Native.App.Platform;
using Waller.Native.Core.Settings;

namespace Waller.Native.App.ViewModels;

internal sealed record SettingsPreferenceSaveResult(
    Guid? LastSelectedPresetId,
    bool WriteFailed)
{
    public static SettingsPreferenceSaveResult Success(Guid? lastSelectedPresetId) =>
        new(lastSelectedPresetId, WriteFailed: false);

    public static SettingsPreferenceSaveResult LocalWriteFailed() =>
        new(LastSelectedPresetId: null, WriteFailed: true);

    public string StatusText(ShellStatusTextPresenter shellText) =>
        WriteFailed
            ? shellText.LocalDataWriteFailed
            : shellText.SettingsSaved;

    public bool TryGetSavedLastSelectedPresetId(out Guid? lastSelectedPresetId)
    {
        lastSelectedPresetId = LastSelectedPresetId;
        return !WriteFailed;
    }
}

internal static class SettingsPreferenceStore
{
    public static async Task<SettingsPreferenceDraft> LoadDraftAsync(UserSettingsStore settingsStore) =>
        SettingsPreferenceDraft.From(await settingsStore.LoadAsync());

    public static async Task<SettingsPreferenceSaveResult> SaveRequestAsync(
        UserSettingsStore settingsStore,
        SettingsSaveRequest request)
    {
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
        return await LocalDataWriteGuard.TryAsync<Guid?>(
            async () =>
            {
                var settings = await settingsStore.LoadAsync();
                await settingsStore.SaveAsync(settings.WithLastSelectedPreset(presetId));
                return presetId;
            },
            fallback: null);
    }
}
