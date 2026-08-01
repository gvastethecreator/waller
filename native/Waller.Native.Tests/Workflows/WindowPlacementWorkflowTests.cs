using Waller.Native.Core.Settings;
using Waller.Native.Workflows.Settings;
using Waller.Native.Workflows.Windowing;

namespace Waller.Native.Tests.Workflows;

public sealed class WindowPlacementWorkflowTests
{
    [Fact]
    public async Task RestoreAsync_ReturnsSavedPlacementInsideWorkArea()
    {
        var root = CreateRoot();
        try
        {
            var store = new UserSettingsStore(root);
            await store.SaveAsync(UserSettings.Default.WithWindowPlacement(1280, 720, -20, 40));
            var workflow = CreateWorkflow(store);

            var placement = await workflow.RestoreAsync(new WindowWorkArea(-1920, 0, 3840, 1080));

            Assert.Equal(new WindowPlacement(1280, 720, -20, 40), placement);
        }
        finally
        {
            DeleteRoot(root);
        }
    }

    [Fact]
    public async Task RestoreAsync_CentersIncompletePlacementWithExistingPolicy()
    {
        var root = CreateRoot();
        try
        {
            var store = new UserSettingsStore(root);
            await store.SaveAsync(UserSettings.Default with
            {
                WindowWidth = 900,
                WindowHeight = 600,
                WindowX = 50,
                WindowY = null,
            });
            var workflow = CreateWorkflow(store);

            var placement = await workflow.RestoreAsync(new WindowWorkArea(100, 50, 1920, 1040));

            Assert.Equal(new WindowPlacement(1536, 1024, 292, 58), placement);
        }
        finally
        {
            DeleteRoot(root);
        }
    }

    [Fact]
    public async Task SaveAsync_PersistsCompleteGeometryThroughSettingsWorkflow()
    {
        var root = CreateRoot();
        try
        {
            var store = new UserSettingsStore(root);
            var presetId = Guid.NewGuid();
            await store.SaveAsync(UserSettings.Default.WithPreferences(
                AppThemePreference.Light,
                "es",
                presetId));
            var workflow = CreateWorkflow(store);

            var result = await workflow.SaveAsync(new WindowPlacement(1400, 860, -120, 75));
            var loaded = await store.LoadAsync();

            Assert.True(result.Succeeded);
            Assert.Equal((1400, 860, -120, 75),
                (loaded.WindowWidth, loaded.WindowHeight, loaded.WindowX, loaded.WindowY));
            Assert.Equal(AppThemePreference.Light, loaded.Theme);
            Assert.Equal("es", loaded.Language);
            Assert.Equal(presetId, loaded.LastSelectedPresetId);
        }
        finally
        {
            DeleteRoot(root);
        }
    }

    [Theory]
    [InlineData(0, 900)]
    [InlineData(1400, 0)]
    public async Task SaveAsync_RejectsInvalidGeometry(int width, int height)
    {
        var root = CreateRoot();
        try
        {
            var workflow = CreateWorkflow(new UserSettingsStore(root));

            await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
                workflow.SaveAsync(new WindowPlacement(width, height, 10, 20)));

            Assert.False(Directory.Exists(root));
        }
        finally
        {
            DeleteRoot(root);
        }
    }

    private static WindowPlacementWorkflow CreateWorkflow(UserSettingsStore store) =>
        new(new UserSettingsWorkflow(store));

    private static string CreateRoot() =>
        Path.Combine(Path.GetTempPath(), $"waller-window-workflow-{Guid.NewGuid():N}");

    private static void DeleteRoot(string root)
    {
        if (Directory.Exists(root))
        {
            Directory.Delete(root, recursive: true);
        }
    }
}
