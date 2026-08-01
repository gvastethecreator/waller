using Waller.Native.App.Platform;
using Waller.Native.Core.Rendering;
using Waller.Native.Workflows.Settings;

namespace Waller.Native.App.ViewModels;

internal sealed class MainPageLocalState
{
    private readonly WallerLocalDataStores stores;
    private readonly UserSettingsWorkflow userSettings;

    public MainPageLocalState(
        WallerLocalDataStores stores,
        UserSettingsWorkflow userSettings)
    {
        ArgumentNullException.ThrowIfNull(stores);
        ArgumentNullException.ThrowIfNull(userSettings);
        this.stores = stores;
        this.userSettings = userSettings;
    }

    public async Task<SettingsPreferenceDraft> LoadSettingsDraftAsync() =>
        SettingsPreferenceDraft.From(await userSettings.LoadAsync());

    public async Task<SettingsPreferenceSaveResult> SaveSettingsAsync(SettingsSaveRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        var result = await userSettings.UpdatePreferencesAsync(
            request.Theme,
            request.Language,
            request.LastSelectedPresetId);
        return result.TryGetUpdatedSettings(out var updated)
            ? SettingsPreferenceSaveResult.Success(updated.LastSelectedPresetId)
            : SettingsPreferenceSaveResult.LocalWriteFailed();
    }

    public async Task<Guid?> PersistLastSelectedPresetAsync(Guid? presetId)
    {
        var result = await userSettings.UpdateLastSelectedPresetAsync(presetId);
        return result.TryGetUpdatedSettings(out var updated)
            ? updated.LastSelectedPresetId
            : null;
    }

    public RenderedCacheClearResult ClearRenderedCache() =>
        RenderedCacheCleanup.Clear(stores.RenderedWallpapers);
}
