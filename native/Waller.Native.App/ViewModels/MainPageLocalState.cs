using System.Collections.ObjectModel;
using Waller.Native.App.Platform;
using Waller.Native.Core.Models;
using Waller.Native.Core.Presets;
using Waller.Native.Core.Rendering;

namespace Waller.Native.App.ViewModels;

internal sealed class MainPageLocalState(WallerLocalDataStores stores)
{
    public Task<SettingsPreferenceDraft> LoadSettingsDraftAsync() =>
        SettingsPreferenceStore.LoadDraftAsync(stores.Settings);

    public Task<SettingsPreferenceSaveResult> SaveSettingsAsync(SettingsSaveRequest request) =>
        SettingsPreferenceStore.SaveRequestAsync(stores.Settings, request);

    public Task<Guid?> PersistLastSelectedPresetAsync(Guid? presetId) =>
        SettingsPreferenceStore.PersistLastSelectedPresetAsync(stores.Settings, presetId);

    public Task<PresetSessionSaveResult> SaveExistingPresetAsync(
        ActiveSession session,
        Preset selectedPresetRecord) =>
        PresetSessionSave.SaveExistingAsync(stores.Presets, session, selectedPresetRecord);

    public Task<PresetSessionSaveResult> SaveAsPresetAsync(
        ActiveSession session,
        string name) =>
        PresetSessionSave.SaveAsAsync(stores.Presets, session, name);

    public Task<PresetMenuRefreshResult> RefreshMainPresetsAsync(
        ObservableCollection<PresetMenuItem> items,
        string currentSetupName,
        Guid? selectPresetId) =>
        PresetMenuRefresh.RefreshMainAsync(stores.Presets, items, currentSetupName, selectPresetId);

    public Task<PresetMenuItem?> RefreshManagedPresetsAsync(
        ObservableCollection<PresetMenuItem> items,
        Guid? selectPresetId) =>
        ManagedPresetList.RefreshAsync(stores.Presets, items, selectPresetId);

    public Task<SelectedPresetLoadResult> LoadSelectedPresetAsync(
        PresetMatcher presetMatcher,
        ActiveSession activeSession,
        PresetMenuItem item) =>
        SelectedPresetSessionLoader.LoadAsync(stores.Presets, presetMatcher, activeSession, item);

    public Task<ManagedPresetMutationResult<Preset>> RenameManagedPresetAsync(Guid presetId, string name) =>
        ManagedPresetMutation.RenameAsync(stores.Presets, presetId, name);

    public Task<ManagedPresetMutationResult<Preset>> DuplicateManagedPresetAsync(Guid presetId, string name) =>
        ManagedPresetMutation.DuplicateAsync(stores.Presets, presetId, name);

    public Task<ManagedPresetDeleteResult> DeleteManagedPresetAsync(
        ActiveSession activeSession,
        PresetDeleteConfirmation target) =>
        ManagedPresetDelete.DeleteAsync(stores.Presets, activeSession, target);

    public RenderedCacheClearResult ClearRenderedCache() =>
        RenderedCacheCleanup.Clear(stores.RenderedWallpapers);
}
