using Waller.Native.Core.Settings;
using Waller.Native.Workflows.Settings;

namespace Waller.Native.Tests.Workflows;

public sealed class UserSettingsWorkflowTests
{
    [Fact]
    public async Task UpdatePreferences_PreservesWindowPlacement()
    {
        var root = CreateRoot();
        try
        {
            var store = new UserSettingsStore(root);
            var presetId = Guid.NewGuid();
            await store.SaveAsync(UserSettings.Default
                .WithWindowPlacement(1280, 720, -30, 45)
                .WithLastSelectedPreset(presetId));
            var workflow = new UserSettingsWorkflow(store);

            var result = await workflow.UpdatePreferencesAsync(
                AppThemePreference.Light,
                "es",
                presetId);
            var loaded = await store.LoadAsync();

            Assert.True(result.Succeeded);
            Assert.Equal(AppThemePreference.Light, loaded.Theme);
            Assert.Equal("en", loaded.Language);
            Assert.Equal(presetId, loaded.LastSelectedPresetId);
            Assert.Equal((1280, 720, -30, 45),
                (loaded.WindowWidth, loaded.WindowHeight, loaded.WindowX, loaded.WindowY));
        }
        finally
        {
            DeleteRoot(root);
        }
    }

    [Fact]
    public async Task UpdateWindowPlacement_PreservesPreferencesAndPreset()
    {
        var root = CreateRoot();
        try
        {
            var store = new UserSettingsStore(root);
            var presetId = Guid.NewGuid();
            await store.SaveAsync(UserSettings.Default.WithPreferences(
                AppThemePreference.System,
                "es",
                presetId));
            var workflow = new UserSettingsWorkflow(store);

            var result = await workflow.UpdateWindowPlacementAsync(1440, 900, 20, -10);
            var loaded = await store.LoadAsync();

            Assert.True(result.Succeeded);
            Assert.Equal(AppThemePreference.System, loaded.Theme);
            Assert.Equal("en", loaded.Language);
            Assert.Equal(presetId, loaded.LastSelectedPresetId);
            Assert.Equal((1440, 900, 20, -10),
                (loaded.WindowWidth, loaded.WindowHeight, loaded.WindowX, loaded.WindowY));
        }
        finally
        {
            DeleteRoot(root);
        }
    }

    [Fact]
    public async Task UpdateLastSelectedPreset_PreservesPreferencesAndWindowPlacement()
    {
        var root = CreateRoot();
        try
        {
            var store = new UserSettingsStore(root);
            await store.SaveAsync(UserSettings.Default
                .WithPreferences(AppThemePreference.Light, "es", null)
                .WithWindowPlacement(1360, 820, -80, 65));
            var workflow = new UserSettingsWorkflow(store);
            var presetId = Guid.NewGuid();

            var result = await workflow.UpdateLastSelectedPresetAsync(presetId);
            var loaded = await store.LoadAsync();

            Assert.True(result.Succeeded);
            Assert.Equal(AppThemePreference.Light, loaded.Theme);
            Assert.Equal("en", loaded.Language);
            Assert.Equal(presetId, loaded.LastSelectedPresetId);
            Assert.Equal((1360, 820, -80, 65),
                (loaded.WindowWidth, loaded.WindowHeight, loaded.WindowX, loaded.WindowY));
        }
        finally
        {
            DeleteRoot(root);
        }
    }

    [Fact]
    public async Task ConcurrentUpdates_RunInEnqueueOrderWithoutLosingFields()
    {
        var root = CreateRoot();
        try
        {
            var store = new UserSettingsStore(root);
            var workflow = new UserSettingsWorkflow(store);
            var firstPresetId = Guid.NewGuid();
            var finalPresetId = Guid.NewGuid();

            var preferences = workflow.UpdatePreferencesAsync(
                AppThemePreference.Light,
                "es",
                firstPresetId);
            var placement = workflow.UpdateWindowPlacementAsync(1500, 940, 12, 34);
            var finalPreset = workflow.UpdateLastSelectedPresetAsync(finalPresetId);

            var results = await Task.WhenAll(preferences, placement, finalPreset);
            var loaded = await workflow.LoadAsync();

            Assert.All(results, result => Assert.True(result.Succeeded));
            Assert.Equal(AppThemePreference.Light, loaded.Theme);
            Assert.Equal("en", loaded.Language);
            Assert.Equal(finalPresetId, loaded.LastSelectedPresetId);
            Assert.Equal((1500, 940, 12, 34),
                (loaded.WindowWidth, loaded.WindowHeight, loaded.WindowX, loaded.WindowY));
        }
        finally
        {
            DeleteRoot(root);
        }
    }

    [Fact]
    public async Task RecoverableStorageFailure_ReturnsTypedResult()
    {
        var root = CreateRoot();
        try
        {
            Directory.CreateDirectory(Path.Combine(root, "settings.json"));
            var workflow = new UserSettingsWorkflow(new UserSettingsStore(root));

            var result = await workflow.UpdateLastSelectedPresetAsync(Guid.NewGuid());

            Assert.False(result.Succeeded);
            Assert.Equal(UserSettingsUpdateError.LocalStorageUnavailable, result.Error);
            Assert.Null(result.UpdatedSettings);
        }
        finally
        {
            DeleteRoot(root);
        }
    }

    [Fact]
    public async Task CancelledUpdate_DoesNotPublishOrLeaveTemporaryFile()
    {
        var root = CreateRoot();
        try
        {
            var store = new UserSettingsStore(root);
            var original = UserSettings.Default
                .WithPreferences(AppThemePreference.Light, "es", Guid.NewGuid())
                .WithWindowPlacement(1280, 760, 8, 16);
            await store.SaveAsync(original);
            var workflow = new UserSettingsWorkflow(store);
            using var cancellation = new CancellationTokenSource();
            cancellation.Cancel();

            await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
                workflow.UpdatePreferencesAsync(
                    AppThemePreference.Dark,
                    "en",
                    null,
                    cancellation.Token));

            Assert.Equal(original, await store.LoadAsync());
            Assert.Empty(Directory.EnumerateFiles(root, "*.tmp"));
        }
        finally
        {
            DeleteRoot(root);
        }
    }

    private static string CreateRoot() =>
        Path.Combine(Path.GetTempPath(), $"waller-settings-workflow-{Guid.NewGuid():N}");

    private static void DeleteRoot(string root)
    {
        if (Directory.Exists(root))
        {
            Directory.Delete(root, recursive: true);
        }
    }
}
