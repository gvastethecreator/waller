using Waller.Native.Core.Models;
using Waller.Native.Core.Presets;
using Waller.Native.Workflows.Presets;

namespace Waller.Native.Tests.Workflows;

public sealed class PresetWorkflowTests
{
    [Fact]
    public async Task CompleteFlow_ListsSelectsSavesRenamesDuplicatesAndDeletes()
    {
        var root = CreateRoot();
        try
        {
            var workflow = new PresetWorkflow(new PresetStore(root));
            var sourceSession = CreateSession("#112233");

            var saved = await workflow.SaveAsAsync(sourceSession, "Desk");
            Assert.True(saved.TryGetValue(out var preset));
            Assert.Single(await workflow.ListAsync());

            var selected = await workflow.SelectAsync(CreateSession("#000000"), preset.Id);
            Assert.True(selected.TryGetValue(out var selection));
            Assert.Equal(preset.Id, selection.Session.BasedOnPreset?.Id);
            Assert.Equal("#112233", selection.Session.Monitors[0].DesiredAssignment.Source.ColorHex);

            var updatedSession = selection.Session with { HasUnsavedPresetChanges = true };
            var originalPresetId = preset.Id;
            var updated = await workflow.SaveExistingAsync(updatedSession, preset);
            Assert.True(updated.TryGetValue(out var updatedPreset));
            Assert.Equal(originalPresetId, updatedPreset.Id);
            preset = updatedPreset;

            var renamed = await workflow.RenameAsync(preset.Id, "Studio");
            Assert.True(renamed.TryGetValue(out var renamedPreset));
            Assert.Equal("Studio", renamedPreset.Name);

            var duplicated = await workflow.DuplicateAsync(preset.Id, "Studio variant");
            Assert.True(duplicated.TryGetValue(out var duplicate));
            Assert.NotEqual(preset.Id, duplicate.Id);
            Assert.Equal("Studio variant copy", duplicate.Name);

            var deleted = await workflow.DeleteAsync(selection.Session, duplicate.Id);
            Assert.True(deleted.Succeeded);
            Assert.Single(await workflow.ListAsync());
        }
        finally
        {
            DeleteRoot(root);
        }
    }

    [Fact]
    public async Task SelectCurrentSetup_PreservesMonitorStateWithoutApplyDependency()
    {
        var root = CreateRoot();
        try
        {
            var workflow = new PresetWorkflow(new PresetStore(root));
            var saved = await workflow.SaveAsAsync(CreateSession("#112233"), "Desk");
            Assert.True(saved.TryGetValue(out var preset));
            var current = CreateSession("#445566") with
            {
                BasedOnPreset = preset.Identity,
                HasUnsavedPresetChanges = true,
            };

            var result = await workflow.SelectAsync(current, presetId: null);

            Assert.True(result.TryGetValue(out var selection));
            Assert.True(selection.IsCurrentSetup);
            Assert.Equal(current.Monitors, selection.Session.Monitors);
            Assert.Null(selection.Session.BasedOnPreset);
            Assert.False(selection.Session.HasUnsavedPresetChanges);
        }
        finally
        {
            DeleteRoot(root);
        }
    }

    [Fact]
    public async Task MissingPreset_ProducesTypedOutcomes()
    {
        var root = CreateRoot();
        try
        {
            var workflow = new PresetWorkflow(new PresetStore(root));
            var missingId = Guid.NewGuid();
            var session = CreateSession("#112233");

            var select = await workflow.SelectAsync(session, missingId);
            var rename = await workflow.RenameAsync(missingId, "Missing");
            var duplicate = await workflow.DuplicateAsync(missingId, null);
            var delete = await workflow.DeleteAsync(session, missingId);

            Assert.Equal(PresetOperationStatus.Missing, select.Status);
            Assert.Equal(PresetOperationStatus.Missing, rename.Status);
            Assert.Equal(PresetOperationStatus.Missing, duplicate.Status);
            Assert.Equal(PresetOperationStatus.Missing, delete.Status);
        }
        finally
        {
            DeleteRoot(root);
        }
    }

    [Fact]
    public async Task WriteFailure_ProducesTypedOutcome()
    {
        var root = CreateRoot();
        try
        {
            Directory.CreateDirectory(root);
            await File.WriteAllTextAsync(Path.Combine(root, "presets"), "blocked");
            var workflow = new PresetWorkflow(new PresetStore(root));

            var result = await workflow.SaveAsAsync(CreateSession("#112233"), "Desk");

            Assert.Equal(PresetOperationStatus.WriteFailed, result.Status);
            Assert.Null(result.Value);
        }
        finally
        {
            DeleteRoot(root);
        }
    }

    [Fact]
    public async Task DeleteActivePreset_PreservesSessionAndMarksItUnsaved()
    {
        var root = CreateRoot();
        try
        {
            var workflow = new PresetWorkflow(new PresetStore(root));
            var saved = await workflow.SaveAsAsync(CreateSession("#112233"), "Desk");
            Assert.True(saved.TryGetValue(out var preset));
            var selected = await workflow.SelectAsync(CreateSession("#000000"), preset.Id);
            Assert.True(selected.TryGetValue(out var selection));

            var deleted = await workflow.DeleteAsync(selection.Session, preset.Id);

            Assert.True(deleted.TryGetValue(out var deletion));
            Assert.True(deletion.DeletedActivePreset);
            Assert.Equal(selection.Session.Monitors, deletion.Session.Monitors);
            Assert.Equal(selection.Session.MissingAssignments, deletion.Session.MissingAssignments);
            Assert.Null(deletion.Session.BasedOnPreset);
            Assert.True(deletion.Session.HasUnsavedPresetChanges);
        }
        finally
        {
            DeleteRoot(root);
        }
    }

    private static ActiveSession CreateSession(string color) =>
        ActiveSession.FromMonitors(
        [
            new MonitorSnapshot(
                new MonitorIdentity("DISPLAY-1", "DISPLAY1", 1, 1920, 1080, 0, 0),
                "Display 1",
                WallpaperSource.FromSolidColor(color),
                WallpaperPlacement.Default),
        ]);

    private static string CreateRoot() =>
        Path.Combine(Path.GetTempPath(), $"waller-preset-workflow-{Guid.NewGuid():N}");

    private static void DeleteRoot(string root)
    {
        if (Directory.Exists(root))
        {
            Directory.Delete(root, recursive: true);
        }
    }
}
