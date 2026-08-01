using Waller.Native.Core.Models;
using Waller.Native.Workflows.MonitorEditing;

namespace Waller.Native.Tests.Workflows;

public sealed class MonitorEditorWorkflowTests
{
    private readonly MonitorEditorWorkflow workflow = new();

    [Fact]
    public void Select_CreatesDraftFromDesiredAssignment()
    {
        var session = CreateSession(WallpaperSource.FromSolidColor("#112233"));

        var result = workflow.Select(session, "DISPLAY-1");

        Assert.True(result.TryGetDraft(out var draft));
        Assert.Equal(WallpaperSourceKind.SolidColor, draft.SourceKind);
        Assert.Equal("#112233", draft.ColorHex);
        Assert.Equal(WallpaperPlacement.Default.FitMode, draft.FitMode);
    }

    [Fact]
    public void UpdateSource_ReturnsTypedOutcomeAndOneReplacementSession()
    {
        var session = CreateSession(WallpaperSource.Empty);
        var draft = new MonitorEditorDraft(
            WallpaperSourceKind.SolidColor,
            ImagePath: null,
            ColorHex: "#445566",
            WallpaperFitMode.Cover,
            WallpaperAnchor.Center);

        var result = workflow.Update(session, "DISPLAY-1", draft);

        Assert.Equal(MonitorEditorStatus.Updated, result.Status);
        Assert.True(result.TryGetUpdatedSession(out var updated));
        Assert.NotSame(session, updated);
        Assert.Equal("#445566", updated.Monitors[0].DesiredAssignment.Source.ColorHex);
        Assert.True(updated.HasUnsavedPresetChanges);
    }

    [Fact]
    public void UpdatePlacement_UsesCoreOffsetNormalization()
    {
        var session = CreateSession(WallpaperSource.Empty);
        var draft = new MonitorEditorDraft(
            WallpaperSourceKind.Empty,
            ImagePath: null,
            ColorHex: null,
            WallpaperFitMode.Tile,
            WallpaperAnchor.BottomRight,
            OffsetXPercent: 150.4,
            OffsetYPercent: -42.5);

        var result = workflow.Update(session, "DISPLAY-1", draft);

        Assert.True(result.TryGetUpdatedSession(out var updated));
        var placement = updated.Monitors[0].DesiredAssignment.Placement;
        Assert.Equal(WallpaperFitMode.Tile, placement.FitMode);
        Assert.Equal(WallpaperAnchor.BottomRight, placement.Anchor);
        Assert.Equal(100, placement.OffsetXPercent);
        Assert.Equal(-43, placement.OffsetYPercent);
    }

    [Fact]
    public void MissingImage_ReturnsFailureWithoutChangingSession()
    {
        var session = CreateSession(WallpaperSource.Empty);
        var missingPath = Path.Combine(
            Path.GetTempPath(),
            $"waller-missing-{Guid.NewGuid():N}.png");
        var draft = new MonitorEditorDraft(
            WallpaperSourceKind.Image,
            missingPath,
            ColorHex: null,
            WallpaperFitMode.Cover,
            WallpaperAnchor.Center);

        var result = workflow.Update(session, "DISPLAY-1", draft);

        Assert.Equal(MonitorEditorStatus.ImageMissing, result.Status);
        Assert.Equal(missingPath, result.MissingImagePath);
        Assert.False(result.TryGetUpdatedSession(out _));
        Assert.Equal(WallpaperSourceKind.Empty, session.Monitors[0].DesiredAssignment.Source.Kind);
    }

    [Fact]
    public void ForgetDisconnected_RemovesOnlyRequestedAssignment()
    {
        var session = WithMissingAssignments(
            CreateSession(WallpaperSource.Empty),
            MissingAssignment("MISSING-1", "#111111"),
            MissingAssignment("MISSING-2", "#222222"));

        var result = workflow.ForgetDisconnected(session, "MISSING-1");

        Assert.True(result.TryGetUpdatedSession(out var updated));
        Assert.Single(updated.MissingAssignments);
        Assert.Equal("MISSING-2", updated.MissingAssignments[0].SavedMonitor.MonitorKey);
        Assert.Equal(session.Monitors, updated.Monitors);
    }

    [Fact]
    public void ReassignDisconnected_MovesAssignmentToCurrentTopology()
    {
        var session = WithMissingAssignments(
            CreateSession(WallpaperSource.Empty),
            MissingAssignment("MISSING-1", "#abcdef"));

        var result = workflow.ReassignDisconnected(session, "MISSING-1", "DISPLAY-1");

        Assert.True(result.TryGetUpdatedSession(out var updated));
        Assert.Empty(updated.MissingAssignments);
        Assert.Equal("#abcdef", updated.Monitors[0].DesiredAssignment.Source.ColorHex);
        Assert.Equal("DISPLAY-1", updated.Monitors[0].DesiredAssignment.SavedMonitor.MonitorKey);
    }

    [Fact]
    public void ReassignDisconnected_RejectsMissingTargetTopology()
    {
        var session = WithMissingAssignments(
            CreateSession(WallpaperSource.Empty),
            MissingAssignment("MISSING-1", "#abcdef"));

        var result = workflow.ReassignDisconnected(session, "MISSING-1", "DISPLAY-9");

        Assert.Equal(MonitorEditorStatus.TargetMonitorMissing, result.Status);
        Assert.False(result.TryGetUpdatedSession(out _));
        Assert.Single(session.MissingAssignments);
    }

    private static ActiveSession CreateSession(WallpaperSource source) =>
        ActiveSession.FromMonitors(
        [
            new MonitorSnapshot(
                new MonitorIdentity("DISPLAY-1", "DISPLAY1", 1, 1920, 1080, 0, 0),
                "Display 1",
                source,
                WallpaperPlacement.Default),
        ]);

    private static ActiveSession WithMissingAssignments(
        ActiveSession session,
        params PresetAssignment[] missingAssignments) =>
        session with { MissingAssignments = missingAssignments };

    private static PresetAssignment MissingAssignment(string monitorKey, string color) =>
        new(
            new MonitorIdentity(monitorKey, monitorKey, 8, 1280, 720, 1920, 0),
            WallpaperSource.FromSolidColor(color),
            WallpaperPlacement.Default);
}
