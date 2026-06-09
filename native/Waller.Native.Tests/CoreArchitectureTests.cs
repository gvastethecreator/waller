using Waller.Native.Core.Models;
using Waller.Native.Core.Presets;
using Waller.Native.Core.Rendering;
using Waller.Native.Core.Sessions;
using Waller.Native.Core.Settings;
using Waller.Native.Core.Storage;
using Waller.Native.Core.Topology;
using Waller.Native.Core.Windows;

namespace Waller.Native.Tests;

public sealed class CoreArchitectureTests
{
    [Fact]
    public async Task ActiveSessionFactory_UsesDetectedWindowsState()
    {
        var factory = new ActiveSessionFactory(new SampleMonitorDetector());

        var session = await factory.CreateFromCurrentWindowsStateAsync();

        Assert.Equal(3, session.Monitors.Count);
        Assert.Null(session.BasedOnPreset);
        Assert.False(session.HasUnsavedPresetChanges);
        Assert.All(session.Monitors, monitor => Assert.Equal(MonitorApplyStatus.Clean, monitor.ApplyStatus));
    }

    [Fact]
    public async Task ActiveSessionFactory_UsesDetectedWindowsPlacement()
    {
        var monitor = new MonitorSnapshot(
            new MonitorIdentity("DISPLAY-1", "Monitor 1", 1, 1920, 1080, 0, 0),
            "Monitor 1",
            WallpaperSource.FromImage(@"C:\Wallpapers\current.jpg"),
            new WallpaperPlacement(WallpaperFitMode.Contain, WallpaperAnchor.Center));
        var factory = new ActiveSessionFactory(new FixedMonitorDetector([monitor]));

        var session = await factory.CreateFromCurrentWindowsStateAsync();

        Assert.Equal(WallpaperFitMode.Contain, session.Monitors[0].DesiredAssignment.Placement.FitMode);
        Assert.Equal(WallpaperAnchor.Center, session.Monitors[0].DesiredAssignment.Placement.Anchor);
    }

    [Fact]
    public async Task ActiveSessionFactory_NormalizesDetectedPlacementOffsets()
    {
        var monitor = new MonitorSnapshot(
            new MonitorIdentity("DISPLAY-1", "Monitor 1", 1, 1920, 1080, 0, 0),
            "Monitor 1",
            WallpaperSource.FromImage(@"C:\Wallpapers\current.jpg"),
            new WallpaperPlacement(WallpaperFitMode.Cover, WallpaperAnchor.Center, 240, -240));
        var factory = new ActiveSessionFactory(new FixedMonitorDetector([monitor]));

        var session = await factory.CreateFromCurrentWindowsStateAsync();

        Assert.Equal(100, session.Monitors[0].DesiredAssignment.Placement.OffsetXPercent);
        Assert.Equal(-100, session.Monitors[0].DesiredAssignment.Placement.OffsetYPercent);
        Assert.Equal(session.Monitors[0].DesiredAssignment, session.Monitors[0].LastAppliedAssignment);
    }

    [Fact]
    public async Task ActiveSessionFactory_AllowsEmptyMonitorFallback()
    {
        var factory = new ActiveSessionFactory(new EmptyMonitorDetector());

        var session = await factory.CreateFromCurrentWindowsStateAsync();

        Assert.Empty(session.Monitors);
        Assert.Null(session.BasedOnPreset);
        Assert.False(session.HasUnsavedPresetChanges);
    }

    [Fact]
    public async Task ActiveSessionEditor_UpdatesDesiredAssignmentWithoutApplying()
    {
        var factory = new ActiveSessionFactory(new SampleMonitorDetector());
        var editor = new ActiveSessionEditor();
        var session = await factory.CreateFromCurrentWindowsStateAsync();
        var monitorKey = session.Monitors[0].Monitor.Identity.MonitorKey;

        var next = editor.UpdateAssignment(
            session,
            monitorKey,
            WallpaperSource.FromSolidColor("#336699"),
            new WallpaperPlacement(WallpaperFitMode.Contain, WallpaperAnchor.Top));

        var edited = next.Monitors[0];
        Assert.True(next.HasUnsavedPresetChanges);
        Assert.True(edited.HasUnsavedPresetChanges);
        Assert.Equal(MonitorApplyStatus.Pending, edited.ApplyStatus);
        Assert.Equal(WallpaperSourceKind.SolidColor, edited.DesiredAssignment.Source.Kind);
        Assert.Equal(WallpaperFitMode.Contain, edited.DesiredAssignment.Placement.FitMode);
        Assert.Equal(MonitorApplyStatus.Clean, session.Monitors[0].ApplyStatus);
    }

    [Fact]
    public async Task ActiveSessionEditor_NormalizesPlacementOffsets()
    {
        var factory = new ActiveSessionFactory(new SampleMonitorDetector());
        var editor = new ActiveSessionEditor();
        var session = await factory.CreateFromCurrentWindowsStateAsync();
        var monitorKey = session.Monitors[0].Monitor.Identity.MonitorKey;

        var next = editor.UpdateAssignment(
            session,
            monitorKey,
            WallpaperSource.FromSolidColor("#336699"),
            new WallpaperPlacement(WallpaperFitMode.Cover, WallpaperAnchor.Center, 240, -240));

        Assert.Equal(100, next.Monitors[0].DesiredAssignment.Placement.OffsetXPercent);
        Assert.Equal(-100, next.Monitors[0].DesiredAssignment.Placement.OffsetYPercent);
    }

    [Fact]
    public async Task ActiveSessionEditor_DoesNotDirtySessionWhenAssignmentIsUnchanged()
    {
        var factory = new ActiveSessionFactory(new SampleMonitorDetector());
        var editor = new ActiveSessionEditor();
        var session = await factory.CreateFromCurrentWindowsStateAsync();
        var monitor = session.Monitors[0];

        var next = editor.UpdateAssignment(
            session,
            monitor.Monitor.Identity.MonitorKey,
            monitor.DesiredAssignment.Source,
            monitor.DesiredAssignment.Placement);

        Assert.Same(session, next);
        Assert.False(next.HasUnsavedPresetChanges);
        Assert.False(next.Monitors[0].HasUnsavedPresetChanges);
        Assert.Equal(MonitorApplyStatus.Clean, next.Monitors[0].ApplyStatus);
    }

    [Fact]
    public void MonitorKeys_CreateSetUsesCaseInsensitiveComparer()
    {
        var set = MonitorKeys.CreateSet(["DISPLAY-1"]);

        Assert.Contains("display-1", set);
    }

    [Fact]
    public async Task ActiveSessionEditor_DoesNotDirtySessionWhenMonitorKeyIsUnknown()
    {
        var factory = new ActiveSessionFactory(new SampleMonitorDetector());
        var editor = new ActiveSessionEditor();
        var session = await factory.CreateFromCurrentWindowsStateAsync();

        var next = editor.UpdateAssignment(
            session,
            "MISSING",
            WallpaperSource.FromSolidColor("#112233"),
            WallpaperPlacement.Default);

        Assert.Same(session, next);
        Assert.False(next.HasUnsavedPresetChanges);
        Assert.All(next.Monitors, monitor => Assert.False(monitor.HasUnsavedPresetChanges));
    }

    [Fact]
    public async Task ActiveSession_WithSavedPresetClearsDirtyState()
    {
        var factory = new ActiveSessionFactory(new SampleMonitorDetector());
        var editor = new ActiveSessionEditor();
        var session = await factory.CreateFromCurrentWindowsStateAsync();
        session = editor.UpdateAssignment(
            session,
            session.Monitors[0].Monitor.Identity.MonitorKey,
            WallpaperSource.FromSolidColor("#112233"),
            WallpaperPlacement.Default);
        var preset = new PresetIdentity(Guid.NewGuid(), "Saved preset");

        var saved = session.WithSavedPreset(preset);

        Assert.Equal(preset, saved.BasedOnPreset);
        Assert.False(saved.HasUnsavedPresetChanges);
        Assert.All(saved.Monitors, monitor => Assert.False(monitor.HasUnsavedPresetChanges));
    }

    [Fact]
    public async Task ActiveSessionEditor_RemovesMissingAssignmentWithoutApplying()
    {
        var factory = new ActiveSessionFactory(new SampleMonitorDetector());
        var editor = new ActiveSessionEditor();
        var session = await factory.CreateFromCurrentWindowsStateAsync();
        var missing = new PresetAssignment(
            new MonitorIdentity("MISSING", "Disconnected", 4, 3840, 2160, 0, 0),
            WallpaperSource.Empty,
            WallpaperPlacement.Default);
        session = session with { MissingAssignments = [missing] };

        var next = editor.RemoveMissingAssignment(session, "MISSING");

        Assert.True(next.HasUnsavedPresetChanges);
        Assert.Empty(next.MissingAssignments);
        Assert.All(next.Monitors, monitor => Assert.Equal(MonitorApplyStatus.Clean, monitor.ApplyStatus));
    }

    [Fact]
    public async Task ActiveSessionEditor_ReassignsMissingAssignmentToCurrentMonitor()
    {
        var factory = new ActiveSessionFactory(new SampleMonitorDetector());
        var editor = new ActiveSessionEditor();
        var session = await factory.CreateFromCurrentWindowsStateAsync();
        var targetKey = session.Monitors[1].Monitor.Identity.MonitorKey;
        var missing = new PresetAssignment(
            new MonitorIdentity("MISSING", "Disconnected", 4, 3840, 2160, 0, 0),
            WallpaperSource.FromSolidColor("#445566"),
            new WallpaperPlacement(WallpaperFitMode.Contain, WallpaperAnchor.BottomRight));
        session = session with { MissingAssignments = [missing] };

        var next = editor.ReassignMissingAssignment(session, "MISSING", targetKey);
        var target = next.Monitors[1];

        Assert.True(next.HasUnsavedPresetChanges);
        Assert.Empty(next.MissingAssignments);
        Assert.True(target.HasUnsavedPresetChanges);
        Assert.Equal(MonitorApplyStatus.Pending, target.ApplyStatus);
        Assert.Equal("#445566", target.DesiredAssignment.Source.ColorHex);
        Assert.Equal(WallpaperFitMode.Contain, target.DesiredAssignment.Placement.FitMode);
        Assert.Equal(target.Monitor.Identity.MonitorKey, target.DesiredAssignment.SavedMonitor.MonitorKey);
    }

    [Fact]
    public async Task ActiveSessionEditor_NormalizesReassignedMissingAssignment()
    {
        var factory = new ActiveSessionFactory(new SampleMonitorDetector());
        var editor = new ActiveSessionEditor();
        var session = await factory.CreateFromCurrentWindowsStateAsync();
        var targetKey = session.Monitors[1].Monitor.Identity.MonitorKey;
        var missing = new PresetAssignment(
            new MonitorIdentity("MISSING", "Disconnected", 4, 3840, 2160, 0, 0),
            WallpaperSource.FromSolidColor("#445566"),
            new WallpaperPlacement(WallpaperFitMode.Cover, WallpaperAnchor.Center, 250, -250));
        session = session with { MissingAssignments = [missing] };

        var next = editor.ReassignMissingAssignment(session, "missing", targetKey.ToLowerInvariant());
        var target = next.Monitors[1];

        Assert.Empty(next.MissingAssignments);
        Assert.Equal(target.Monitor.Identity, target.DesiredAssignment.SavedMonitor);
        Assert.Equal(100, target.DesiredAssignment.Placement.OffsetXPercent);
        Assert.Equal(-100, target.DesiredAssignment.Placement.OffsetYPercent);
    }

    [Fact]
    public void ApplyPreflight_MarksMissingImageSourcesAsSkippedErrors()
    {
        var first = CreateMonitor("DISPLAY-1", 16, 16, WallpaperSource.FromImage(@"C:\missing-image-a.png"));
        var second = CreateMonitor("DISPLAY-2", 16, 16, WallpaperSource.FromSolidColor("#112233"));
        var third = CreateMonitor("DISPLAY-3", 16, 16, WallpaperSource.FromImage(@"C:\missing-image-b.png"));
        var session = ActiveSession.FromMonitors([first, second, third]);

        var result = ApplyPreflight.SkipMissingImageSources(session);

        Assert.Equal(2, result.SkippedMonitorKeys.Count);
        Assert.Equal(2, result.SkippedCount);
        Assert.True(result.HasReadyMonitors);
        Assert.True(result.HasSkippedMonitors);
        Assert.Contains("DISPLAY-1", result.SkippedMonitorKeys);
        Assert.Contains("DISPLAY-3", result.SkippedMonitorKeys);
        Assert.Single(result.ReadyMonitorKeys);
        Assert.Contains("DISPLAY-2", result.ReadyMonitorKeys);
        Assert.Equal(MonitorApplyStatus.Error, result.Session.Monitors[0].ApplyStatus);
        Assert.Equal(MonitorApplyStatus.Clean, result.Session.Monitors[1].ApplyStatus);
        Assert.Equal(MonitorApplyStatus.Error, result.Session.Monitors[2].ApplyStatus);
        Assert.Equal(ApplyErrorCodes.MissingImageSource, result.Session.Monitors[0].ApplyError);
    }

    [Fact]
    public void ApplyPreflight_MarksSingleMissingImageSourceAsSkippedError()
    {
        var first = CreateMonitor("DISPLAY-1", 16, 16, WallpaperSource.FromImage(@"C:\missing-image-a.png"));
        var second = CreateMonitor("DISPLAY-2", 16, 16, WallpaperSource.FromImage(@"C:\missing-image-b.png"));
        var session = ActiveSession.FromMonitors([first, second]);

        var result = ApplyPreflight.SkipMissingImageSource(session, "display-2");

        Assert.Single(result.SkippedMonitorKeys);
        Assert.Equal(1, result.SkippedCount);
        Assert.Contains("DISPLAY-2", result.SkippedMonitorKeys);
        Assert.Empty(result.ReadyMonitorKeys);
        Assert.Equal(MonitorApplyStatus.Clean, result.Session.Monitors[0].ApplyStatus);
        Assert.Equal(MonitorApplyStatus.Error, result.Session.Monitors[1].ApplyStatus);
    }

    [Fact]
    public void ApplyPreflight_ReportsSingleReadyImageSource()
    {
        var first = CreateMonitor("DISPLAY-1", 16, 16, WallpaperSource.FromImage(@"C:\missing-image-a.png"));
        var second = CreateMonitor("DISPLAY-2", 16, 16, WallpaperSource.FromSolidColor("#112233"));
        var session = ActiveSession.FromMonitors([first, second]);

        var result = ApplyPreflight.SkipMissingImageSource(session, "display-2");

        Assert.Empty(result.SkippedMonitorKeys);
        Assert.Equal(0, result.SkippedCount);
        Assert.Single(result.ReadyMonitorKeys);
        Assert.True(result.HasReadyMonitors);
        Assert.False(result.HasSkippedMonitors);
        Assert.Contains("DISPLAY-2", result.ReadyMonitorKeys);
        Assert.Same(session, result.Session);
    }

    [Fact]
    public void ApplyPreflight_DoesNotMarkUnknownMonitorAsSkipped()
    {
        var session = ActiveSession.FromMonitors([
            CreateMonitor("DISPLAY-1", 16, 16, WallpaperSource.FromImage(@"C:\missing-image-a.png")),
        ]);

        var result = ApplyPreflight.SkipMissingImageSource(session, "MISSING");

        Assert.Empty(result.SkippedMonitorKeys);
        Assert.Equal(0, result.SkippedCount);
        Assert.Empty(result.ReadyMonitorKeys);
        Assert.False(result.HasReadyMonitors);
        Assert.False(result.HasSkippedMonitors);
        Assert.Same(session, result.Session);
        Assert.Equal(MonitorApplyStatus.Clean, result.Session.Monitors[0].ApplyStatus);
    }

    [Fact]
    public void ApplyPreflightResult_FactoriesNormalizeMonitorKeySets()
    {
        var session = ActiveSession.FromMonitors([
            CreateMonitor("DISPLAY-1", 16, 16, WallpaperSource.Empty),
        ]);

        var result = ApplyPreflightResult.FromSets(
            session,
            readyMonitorKeys: ["display-1"],
            skippedMonitorKeys: ["display-2"]);

        Assert.Contains("DISPLAY-1", result.ReadyMonitorKeys);
        Assert.Contains("DISPLAY-2", result.SkippedMonitorKeys);
        Assert.True(result.ReadyMonitorKeys.Contains("display-1"));
        Assert.True(result.SkippedMonitorKeys.Contains("display-2"));
    }

    [Fact]
    public void ApplyPreflightResult_NoTargetsUsesEmptyKeySets()
    {
        var session = ActiveSession.FromMonitors([
            CreateMonitor("DISPLAY-1", 16, 16, WallpaperSource.Empty),
        ]);

        var result = ApplyPreflightResult.NoTargets(session);

        Assert.False(result.HasReadyMonitors);
        Assert.False(result.HasSkippedMonitors);
        Assert.Empty(result.ReadyMonitorKeys);
        Assert.Empty(result.SkippedMonitorKeys);
        Assert.Same(session, result.Session);
    }

    [Fact]
    public void ApplyTargetPlan_SelectsMonitorCaseInsensitively()
    {
        var first = MonitorSession.FromMonitor(CreateMonitor("DISPLAY-1", 16, 16, WallpaperSource.Empty));
        var second = MonitorSession.FromMonitor(CreateMonitor("DISPLAY-2", 16, 16, WallpaperSource.Empty));
        var plan = ApplyTargetPlan.Monitor("display-2");

        Assert.False(plan.Includes(first));
        Assert.True(plan.Includes(second));
        Assert.Equal(1, plan.Count([first, second]));
    }

    [Fact]
    public void ApplyTargetPlan_SelectsReadyKeysCaseInsensitively()
    {
        var first = MonitorSession.FromMonitor(CreateMonitor("DISPLAY-1", 16, 16, WallpaperSource.Empty));
        var second = MonitorSession.FromMonitor(CreateMonitor("DISPLAY-2", 16, 16, WallpaperSource.Empty));
        var readyKeys = new HashSet<string> { "display-1" };
        var plan = ApplyTargetPlan.ReadyKeys(readyKeys);

        Assert.True(plan.Includes(first));
        Assert.False(plan.Includes(second));
        Assert.Equal(1, plan.Count([first, second]));
    }

    [Fact]
    public void ApplyTargetPlan_ReadyKeysEmptySelectsNoTargets()
    {
        var first = MonitorSession.FromMonitor(CreateMonitor("DISPLAY-1", 16, 16, WallpaperSource.Empty));
        var second = MonitorSession.FromMonitor(CreateMonitor("DISPLAY-2", 16, 16, WallpaperSource.Empty));

        var plan = ApplyTargetPlan.ReadyKeys(new HashSet<string>());

        Assert.False(plan.Includes(first));
        Assert.False(plan.Includes(second));
        Assert.Equal(0, plan.Count([first, second]));
    }

    [Fact]
    public void ApplyTargetPlan_MatchingRequiresPredicate()
    {
        Assert.Throws<ArgumentNullException>(() => ApplyTargetPlan.Matching(null!));
    }

    [Fact]
    public async Task PresetMatcher_MatchesByExactMonitorKey()
    {
        var session = await new ActiveSessionFactory(new SampleMonitorDetector())
            .CreateFromCurrentWindowsStateAsync();
        var target = session.Monitors[1].Monitor.Identity;
        var preset = CreatePreset([
            new PresetAssignment(
                target,
                WallpaperSource.FromImage(@"C:\Wallpapers\preset.png"),
                new WallpaperPlacement(WallpaperFitMode.Cover, WallpaperAnchor.Right)),
        ]);

        var matched = new PresetMatcher().ApplyPreset(session, preset);

        Assert.Equal(preset.Id, matched.BasedOnPreset?.Id);
        Assert.Empty(matched.MissingAssignments);
        Assert.Equal(@"C:\Wallpapers\preset.png", matched.Monitors[1].DesiredAssignment.Source.ImagePath);
        Assert.Equal(WallpaperAnchor.Right, matched.Monitors[1].DesiredAssignment.Placement.Anchor);
    }

    [Fact]
    public async Task PresetMatcher_NormalizesPlacementOffsetsWhenApplyingPreset()
    {
        var session = await new ActiveSessionFactory(new SampleMonitorDetector())
            .CreateFromCurrentWindowsStateAsync();
        var target = session.Monitors[1].Monitor.Identity;
        var preset = CreatePreset([
            new PresetAssignment(
                target,
                WallpaperSource.FromSolidColor("#112233"),
                new WallpaperPlacement(WallpaperFitMode.Cover, WallpaperAnchor.Center, 240, -240)),
        ]);

        var matched = new PresetMatcher().ApplyPreset(session, preset);

        Assert.False(matched.HasUnsavedPresetChanges);
        Assert.Equal(100, matched.Monitors[1].DesiredAssignment.Placement.OffsetXPercent);
        Assert.Equal(-100, matched.Monitors[1].DesiredAssignment.Placement.OffsetYPercent);
    }

    [Fact]
    public void PresetMatcher_FallsBackToResolutionAndClosePositionWhenMonitorKeyChanges()
    {
        var currentMonitor = new MonitorIdentity("DISPLAY-NEW", "Current", 1, 1920, 1080, 0, 0);
        var savedMonitor = new MonitorIdentity("DISPLAY-OLD", "Saved", 1, 1920, 1080, 24, -16);
        var session = ActiveSession.FromMonitors([
            new MonitorSnapshot(currentMonitor, "Current", WallpaperSource.Empty),
        ]);
        var preset = CreatePreset([
            new PresetAssignment(
                savedMonitor,
                WallpaperSource.FromSolidColor("#112233"),
                new WallpaperPlacement(WallpaperFitMode.Contain, WallpaperAnchor.BottomRight)),
        ]);

        var matched = new PresetMatcher().ApplyPreset(session, preset);

        Assert.Empty(matched.MissingAssignments);
        Assert.Equal("#112233", matched.Monitors[0].DesiredAssignment.Source.ColorHex);
        Assert.Equal(WallpaperFitMode.Contain, matched.Monitors[0].DesiredAssignment.Placement.FitMode);
        Assert.Equal(WallpaperAnchor.BottomRight, matched.Monitors[0].DesiredAssignment.Placement.Anchor);
        Assert.Equal("DISPLAY-NEW", matched.Monitors[0].DesiredAssignment.SavedMonitor.MonitorKey);
    }

    [Fact]
    public void PresetMatcher_ChoosesClosestFallbackWhenMultipleCandidatesMatch()
    {
        var currentMonitor = new MonitorIdentity("DISPLAY-NEW", "Current", 1, 1920, 1080, 0, 0);
        var fartherSavedMonitor = new MonitorIdentity("DISPLAY-FARTHER", "Farther", 2, 1920, 1080, 24, 24);
        var closerSavedMonitor = new MonitorIdentity("DISPLAY-CLOSER", "Closer", 3, 1920, 1080, 4, -4);
        var session = ActiveSession.FromMonitors([
            new MonitorSnapshot(currentMonitor, "Current", WallpaperSource.Empty),
        ]);
        var preset = CreatePreset([
            new PresetAssignment(
                fartherSavedMonitor,
                WallpaperSource.FromSolidColor("#111111"),
                WallpaperPlacement.Default),
            new PresetAssignment(
                closerSavedMonitor,
                WallpaperSource.FromSolidColor("#222222"),
                WallpaperPlacement.Default),
        ]);

        var matched = new PresetMatcher().ApplyPreset(session, preset);

        Assert.Equal("#222222", matched.Monitors[0].DesiredAssignment.Source.ColorHex);
        Assert.Single(matched.MissingAssignments);
        Assert.Equal("DISPLAY-FARTHER", matched.MissingAssignments[0].SavedMonitor.MonitorKey);
    }

    [Fact]
    public void MonitorIdentityMatcher_RejectsFallbackOutsidePositionTolerance()
    {
        var currentMonitor = new MonitorIdentity("DISPLAY-NEW", "Current", 1, 1920, 1080, 0, 0);
        var tooFar = new MonitorIdentity("DISPLAY-OLD", "Saved", 1, 1920, 1080, 33, 0);

        Assert.False(MonitorIdentityMatcher.IsFallbackCandidate(tooFar, currentMonitor));
    }

    [Fact]
    public void MonitorTopologyLayout_ScalesNegativeCoordinateTopology()
    {
        var left = new MonitorBounds(-1920, 0, 1920, 1080);
        var primary = new MonitorBounds(0, 0, 2560, 1440);

        var layout = MonitorTopologyLayout.Calculate([left, primary]);
        var leftTile = layout.TileFor(left);
        var primaryTile = layout.TileFor(primary);

        Assert.Equal(-1920, layout.MinX);
        Assert.Equal(0, layout.MinY);
        Assert.True(layout.SurfaceWidth <= 720);
        Assert.True(layout.SurfaceHeight <= 96);
        Assert.Equal(0, leftTile.Left);
        Assert.True(primaryTile.Left > leftTile.Left);
        Assert.Equal(leftTile.Top, primaryTile.Top);
        Assert.True(primaryTile.Width > leftTile.Width);
    }

    [Fact]
    public void MonitorTopologyLayout_UsesStableEmptySurface()
    {
        var layout = MonitorTopologyLayout.Calculate([]);

        Assert.Equal(720, layout.SurfaceWidth);
        Assert.Equal(96, layout.SurfaceHeight);
        Assert.Equal(0, layout.MinX);
        Assert.Equal(0, layout.MinY);
        Assert.Equal(1, layout.Scale);
    }

    [Fact]
    public void PresetNames_ValidatesAndTrimsNames()
    {
        Assert.Equal("Desk", PresetNames.Validate("  Desk  "));
        Assert.Throws<ArgumentException>(() => PresetNames.Validate("   "));
    }

    [Fact]
    public void PresetNames_BuildsDefaultNameFromTimestamp()
    {
        var createdAt = new DateTimeOffset(2026, 6, 8, 14, 35, 0, TimeSpan.Zero);

        Assert.Equal("Preset 2026-06-08 14.35", PresetNames.DefaultName(createdAt));
    }

    [Fact]
    public void PresetNames_BuildsDuplicateNameFromRequestedOrSourceName()
    {
        Assert.Equal("Desk copy", PresetNames.DuplicateName("Desk", requestedName: null));
        Assert.Equal("Focus copy", PresetNames.DuplicateName("Desk", "  Focus  "));
    }

    [Fact]
    public void ColorHexValue_NormalizesAndParsesRgbValues()
    {
        var color = ColorHexValue.Parse(" A1b2C3 ");

        Assert.Equal("#a1b2c3", color.ToHex());
        Assert.Equal(0xa1, color.Red);
        Assert.Equal(0xb2, color.Green);
        Assert.Equal(0xc3, color.Blue);
        Assert.Equal("#a1b2c3", ColorHexValue.Normalize("A1B2C3"));
    }

    [Fact]
    public void ColorHexValue_RejectsInvalidValuesWithoutThrowingInTryParse()
    {
        Assert.Throws<ArgumentException>(() => ColorHexValue.Parse("#12345"));

        Assert.False(ColorHexValue.TryParse("#12345", out _));
        Assert.False(ColorHexValue.TryParse("not-a-color", out _));
        Assert.False(ColorHexValue.TryParse(null, out _));
    }

    [Fact]
    public void WallpaperSourcePath_TryNormalizeImagePathReportsInvalidPaths()
    {
        Assert.False(WallpaperSourcePath.TryNormalizeImagePath("   ", out _));
        Assert.False(WallpaperSourcePath.TryNormalizeImagePath("relative\\wallpaper.png", out _));
        Assert.False(WallpaperSourcePath.TryNormalizeImagePath(@"C:\Wallpapers\notes.txt", out _));

        Assert.True(WallpaperSourcePath.TryNormalizeImagePath(
            @" C:\Wallpapers\current.jpg ",
            out var normalized));
        Assert.Equal(@"C:\Wallpapers\current.jpg", normalized);

        Assert.True(WallpaperSourcePath.TryNormalizeImagePath(
            @"C:\Wallpapers\CURRENT.PNG",
            out var upperExtension));
        Assert.Equal(@"C:\Wallpapers\CURRENT.PNG", upperExtension);
    }

    [Fact]
    public void WallpaperSourcePath_TryNormalizeImagePathReportsErrorCodes()
    {
        Assert.False(WallpaperSourcePath.TryNormalizeImagePath(
            "   ",
            out _,
            out var blankError));
        Assert.Equal(WallpaperSourcePathException.Required, blankError?.ErrorCode);

        Assert.False(WallpaperSourcePath.TryNormalizeImagePath(
            @"wallpapers\current.jpg",
            out _,
            out var relativeError));
        Assert.Equal(WallpaperSourcePathException.FullyQualifiedRequired, relativeError?.ErrorCode);

        Assert.False(WallpaperSourcePath.TryNormalizeImagePath(
            @"C:\Wallpapers\current.txt",
            out _,
            out var unsupportedError));
        Assert.Equal(WallpaperSourcePathException.UnsupportedFileType, unsupportedError?.ErrorCode);

        Assert.True(WallpaperSourcePath.TryNormalizeImagePath(
            @"C:\Wallpapers\current.jpg",
            out var normalized,
            out var validError));
        Assert.Equal(@"C:\Wallpapers\current.jpg", normalized);
        Assert.Null(validError);
    }

    [Fact]
    public void AppLanguages_NormalizesSupportedLanguageCodes()
    {
        Assert.Equal(AppLanguages.English, AppLanguages.NormalizeOrDefault("EN"));
        Assert.Equal(AppLanguages.Spanish, AppLanguages.NormalizeOrDefault("es"));
        Assert.Equal(AppLanguages.English, AppLanguages.NormalizeOrDefault("fr"));
        Assert.Contains(AppLanguages.English, AppLanguages.Supported);
        Assert.Contains(AppLanguages.Spanish, AppLanguages.Supported);
        Assert.Equal("en", AppLanguages.CultureFor("EN").Name);
        Assert.Equal("es", AppLanguages.CultureFor("ES").Name);
        Assert.Equal("en", AppLanguages.CultureFor("fr").Name);
    }

    [Fact]
    public void WallpaperImageFileTypes_ExposeCommonPickerExtensions()
    {
        Assert.Contains(".jpg", WallpaperImageFileTypes.PickerExtensions);
        Assert.Contains(".jpeg", WallpaperImageFileTypes.PickerExtensions);
        Assert.Contains(".png", WallpaperImageFileTypes.PickerExtensions);
        Assert.Contains(".bmp", WallpaperImageFileTypes.PickerExtensions);
        Assert.Contains(".webp", WallpaperImageFileTypes.PickerExtensions);
        Assert.Contains(".gif", WallpaperImageFileTypes.PickerExtensions);
        Assert.Contains(".tif", WallpaperImageFileTypes.PickerExtensions);
        Assert.Contains(".tiff", WallpaperImageFileTypes.PickerExtensions);
        Assert.Contains(".heic", WallpaperImageFileTypes.PickerExtensions);
        Assert.Contains(".heif", WallpaperImageFileTypes.PickerExtensions);
        Assert.All(
            WallpaperImageFileTypes.PickerExtensions,
            extension => Assert.StartsWith(".", extension, StringComparison.Ordinal));
    }

    [Fact]
    public void LocalDataFileSystemErrors_IdentifyRecoverableFileSystemFailures()
    {
        Assert.True(LocalDataFileSystemErrors.IsRecoverable(new IOException("locked")));
        Assert.True(LocalDataFileSystemErrors.IsRecoverable(new UnauthorizedAccessException("denied")));
        Assert.False(LocalDataFileSystemErrors.IsRecoverable(new InvalidOperationException("bug")));
    }

    [Fact]
    public async Task PresetMatcher_PreservesMissingAssignments()
    {
        var session = await new ActiveSessionFactory(new SampleMonitorDetector())
            .CreateFromCurrentWindowsStateAsync();
        var missingMonitor = new MonitorIdentity("MISSING", "Disconnected", 4, 3840, 2160, 0, 0);
        var preset = CreatePreset([
            new PresetAssignment(
                missingMonitor,
                WallpaperSource.Empty,
                WallpaperPlacement.Default),
        ]);

        var matched = new PresetMatcher().ApplyPreset(session, preset);

        Assert.Single(matched.MissingAssignments);
        Assert.Equal("MISSING", matched.MissingAssignments[0].SavedMonitor.MonitorKey);
        Assert.All(matched.Monitors, monitor => Assert.Equal(MonitorApplyStatus.Clean, monitor.ApplyStatus));
    }

    [Fact]
    public async Task PresetMatcher_IgnoresDuplicateAssignmentsForSameMonitorKey()
    {
        var session = await new ActiveSessionFactory(new SampleMonitorDetector())
            .CreateFromCurrentWindowsStateAsync();
        var target = session.Monitors[0].Monitor.Identity;
        var preset = CreatePreset([
            new PresetAssignment(
                target,
                WallpaperSource.FromSolidColor("#112233"),
                WallpaperPlacement.Default),
            new PresetAssignment(
                target,
                WallpaperSource.FromSolidColor("#445566"),
                new WallpaperPlacement(WallpaperFitMode.Stretch, WallpaperAnchor.Bottom)),
        ]);

        var matched = new PresetMatcher().ApplyPreset(session, preset);

        Assert.Empty(matched.MissingAssignments);
        Assert.Equal("#112233", matched.Monitors[0].DesiredAssignment.Source.ColorHex);
        Assert.Equal(WallpaperFitMode.Cover, matched.Monitors[0].DesiredAssignment.Placement.FitMode);
    }

    [Fact]
    public async Task PresetMatcher_IgnoresDuplicateAssignmentsForSameMonitorKeyCaseInsensitively()
    {
        var session = await new ActiveSessionFactory(new SampleMonitorDetector())
            .CreateFromCurrentWindowsStateAsync();
        var target = session.Monitors[0].Monitor.Identity;
        var lowerCaseTarget = target with { MonitorKey = target.MonitorKey.ToLowerInvariant() };
        var preset = CreatePreset([
            new PresetAssignment(
                lowerCaseTarget,
                WallpaperSource.FromSolidColor("#112233"),
                WallpaperPlacement.Default),
            new PresetAssignment(
                target,
                WallpaperSource.FromSolidColor("#445566"),
                new WallpaperPlacement(WallpaperFitMode.Stretch, WallpaperAnchor.Bottom)),
        ]);

        var matched = new PresetMatcher().ApplyPreset(session, preset);

        Assert.Empty(matched.MissingAssignments);
        Assert.Equal("#112233", matched.Monitors[0].DesiredAssignment.Source.ColorHex);
        Assert.Equal(WallpaperFitMode.Cover, matched.Monitors[0].DesiredAssignment.Placement.FitMode);
    }

    [Fact]
    public async Task PresetStore_RoundTripsLocalJson()
    {
        var root = Path.Combine(Path.GetTempPath(), $"waller-native-tests-{Guid.NewGuid():N}");
        try
        {
            var store = new PresetStore(root);
            var monitor = new MonitorIdentity("DISPLAY-1", "Monitor 1", 1, 1920, 1080, 0, 0);
            var preset = CreatePreset([
                new PresetAssignment(
                    monitor,
                    WallpaperSource.FromSolidColor("#112233"),
                    new WallpaperPlacement(WallpaperFitMode.Center, WallpaperAnchor.Center, 25, -50)),
            ]);

            var saved = await store.SaveAsync(preset);
            var listed = await store.ListAsync();

            Assert.Equal(preset.Id, saved.Id);
            Assert.Single(listed);
            Assert.Equal("#112233", listed[0].Assignments[0].Source.ColorHex);
            Assert.Equal(25, listed[0].Assignments[0].Placement.OffsetXPercent);
            Assert.Equal(-50, listed[0].Assignments[0].Placement.OffsetYPercent);
        }
        finally
        {
            if (Directory.Exists(root))
            {
                Directory.Delete(root, recursive: true);
            }
        }
    }

    [Fact]
    public async Task PresetStore_ClampsPlacementOffsetsWhenSaving()
    {
        var root = Path.Combine(Path.GetTempPath(), $"waller-native-tests-{Guid.NewGuid():N}");
        try
        {
            var store = new PresetStore(root);
            var monitor = new MonitorIdentity("DISPLAY-1", "Monitor 1", 1, 1920, 1080, 0, 0);
            var preset = CreatePreset([
                new PresetAssignment(
                    monitor,
                    WallpaperSource.FromSolidColor("#112233"),
                    new WallpaperPlacement(WallpaperFitMode.Cover, WallpaperAnchor.Center, 250, -250)),
            ]);

            var saved = await store.SaveAsync(preset);
            var loaded = await store.LoadAsync(saved.Id);

            Assert.NotNull(loaded);
            Assert.Equal(100, loaded.Assignments[0].Placement.OffsetXPercent);
            Assert.Equal(-100, loaded.Assignments[0].Placement.OffsetYPercent);
        }
        finally
        {
            if (Directory.Exists(root))
            {
                Directory.Delete(root, recursive: true);
            }
        }
    }

    [Fact]
    public void PresetFilePolicy_NormalizesPresetBeforeSave()
    {
        var savedAt = DateTimeOffset.UnixEpoch.AddDays(1);
        var preset = CreatePreset([]) with
        {
            SchemaVersion = 999,
            Name = "  Desk  ",
            CreatedAt = default,
            UpdatedAt = default,
        };

        var normalized = PresetFilePolicy.NormalizeForSave(preset, savedAt);

        Assert.Equal(Preset.CurrentSchemaVersion, normalized.SchemaVersion);
        Assert.Equal("Desk", normalized.Name);
        Assert.Equal(savedAt, normalized.CreatedAt);
        Assert.Equal(savedAt, normalized.UpdatedAt);
    }

    [Fact]
    public async Task PresetStore_NormalizesDuplicateAssignmentsWhenSaving()
    {
        var root = Path.Combine(Path.GetTempPath(), $"waller-native-tests-{Guid.NewGuid():N}");
        try
        {
            var store = new PresetStore(root);
            var monitor = new MonitorIdentity("DISPLAY-1", "Monitor 1", 1, 1920, 1080, 0, 0);
            var preset = CreatePreset([
                new PresetAssignment(
                    monitor,
                    WallpaperSource.FromSolidColor("#112233"),
                    WallpaperPlacement.Default),
                new PresetAssignment(
                    monitor,
                    WallpaperSource.FromSolidColor("#445566"),
                    new WallpaperPlacement(WallpaperFitMode.Stretch, WallpaperAnchor.Bottom)),
            ]);

            var saved = await store.SaveAsync(preset);
            var loaded = await store.LoadAsync(saved.Id);

            Assert.Single(saved.Assignments);
            Assert.NotNull(loaded);
            Assert.Single(loaded.Assignments);
            Assert.Equal("#112233", loaded.Assignments[0].Source.ColorHex);
        }
        finally
        {
            if (Directory.Exists(root))
            {
                Directory.Delete(root, recursive: true);
            }
        }
    }

    [Fact]
    public async Task PresetStore_ListsPresetsInStableNameOrder()
    {
        var root = Path.Combine(Path.GetTempPath(), $"waller-native-tests-{Guid.NewGuid():N}");
        try
        {
            var store = new PresetStore(root);

            await store.SaveAsync(CreatePreset([]) with { Name = "zeta" });
            await store.SaveAsync(CreatePreset([]) with { Name = "Alpha" });
            await store.SaveAsync(CreatePreset([]) with { Name = "beta" });

            var listed = await store.ListAsync();

            Assert.Equal(["Alpha", "beta", "zeta"], listed.Select(preset => preset.Name));
        }
        finally
        {
            if (Directory.Exists(root))
            {
                Directory.Delete(root, recursive: true);
            }
        }
    }

    [Fact]
    public async Task PresetStore_KeepsExistingJsonWhenAtomicSaveCannotReplaceFile()
    {
        var root = Path.Combine(Path.GetTempPath(), $"waller-native-tests-{Guid.NewGuid():N}");
        FileStream? lockedStream = null;

        try
        {
            var store = new PresetStore(root);
            var saved = await store.SaveAsync(CreatePreset([
                new PresetAssignment(
                    new MonitorIdentity("DISPLAY-1", "Monitor 1", 1, 1920, 1080, 0, 0),
                    WallpaperSource.Empty,
                    WallpaperPlacement.Default),
            ]));
            var path = Path.Combine(root, "presets", $"{saved.Id:N}.json");
            lockedStream = new FileStream(path, FileMode.Open, FileAccess.ReadWrite, FileShare.None);

            var error = await Record.ExceptionAsync(() => store.SaveAsync(saved with { Name = "Changed" }));

            Assert.True(error is IOException or UnauthorizedAccessException);
            lockedStream.Dispose();
            lockedStream = null;

            var loaded = await store.LoadAsync(saved.Id);
            var tempFiles = Directory.EnumerateFiles(Path.Combine(root, "presets"), "*.tmp").ToList();

            Assert.Equal("Desk", loaded?.Name);
            Assert.Empty(tempFiles);
        }
        finally
        {
            lockedStream?.Dispose();
            if (Directory.Exists(root))
            {
                Directory.Delete(root, recursive: true);
            }
        }
    }

    [Fact]
    public async Task PresetStore_RenamesAndDeletesLocalJson()
    {
        var root = Path.Combine(Path.GetTempPath(), $"waller-native-tests-{Guid.NewGuid():N}");
        try
        {
            var store = new PresetStore(root);
            var preset = CreatePreset([
                new PresetAssignment(
                    new MonitorIdentity("DISPLAY-1", "Monitor 1", 1, 1920, 1080, 0, 0),
                    WallpaperSource.Empty,
                    WallpaperPlacement.Default),
            ]);

            var saved = await store.SaveAsync(preset);
            var renamed = await store.RenameAsync(saved.Id, "Renamed");
            var loaded = await store.LoadAsync(saved.Id);

            Assert.Equal("Renamed", renamed.Name);
            Assert.Equal("Renamed", loaded?.Name);

            await store.DeleteAsync(saved.Id);
            Assert.Null(await store.LoadAsync(saved.Id));
            Assert.Empty(await store.ListAsync());
        }
        finally
        {
            if (Directory.Exists(root))
            {
                Directory.Delete(root, recursive: true);
            }
        }
    }

    [Fact]
    public async Task PresetStore_DuplicatesPresetWithNewIdentity()
    {
        var root = Path.Combine(Path.GetTempPath(), $"waller-native-tests-{Guid.NewGuid():N}");
        try
        {
            var store = new PresetStore(root);
            var preset = await store.SaveAsync(CreatePreset([
                new PresetAssignment(
                    new MonitorIdentity("DISPLAY-1", "Monitor 1", 1, 1920, 1080, 0, 0),
                    WallpaperSource.FromSolidColor("#445566"),
                    WallpaperPlacement.Default),
            ]));

            var duplicate = await store.DuplicateAsync(preset, "Copy");
            var listed = await store.ListAsync();

            Assert.NotEqual(preset.Id, duplicate.Id);
            Assert.Equal("Copy", duplicate.Name);
            Assert.Equal(2, listed.Count);
            Assert.Contains(listed, item => item.Id == preset.Id);
            Assert.Contains(listed, item => item.Id == duplicate.Id);
        }
        finally
        {
            if (Directory.Exists(root))
            {
                Directory.Delete(root, recursive: true);
            }
        }
    }

    [Fact]
    public async Task PresetStore_SkipsCorruptLocalJsonFiles()
    {
        var root = Path.Combine(Path.GetTempPath(), $"waller-native-tests-{Guid.NewGuid():N}");
        try
        {
            var store = new PresetStore(root);
            var saved = await store.SaveAsync(CreatePreset([
                new PresetAssignment(
                    new MonitorIdentity("DISPLAY-1", "Monitor 1", 1, 1920, 1080, 0, 0),
                    WallpaperSource.Empty,
                    WallpaperPlacement.Default),
            ]));
            var corruptId = Guid.NewGuid();
            var presetsDirectory = Path.Combine(root, "presets");
            await File.WriteAllTextAsync(Path.Combine(presetsDirectory, $"{corruptId:N}.json"), "{ not-json");

            var listed = await store.ListAsync();
            var corrupt = await store.LoadAsync(corruptId);

            Assert.Single(listed);
            Assert.Equal(saved.Id, listed[0].Id);
            Assert.Null(corrupt);
        }
        finally
        {
            if (Directory.Exists(root))
            {
                Directory.Delete(root, recursive: true);
            }
        }
    }

    [Fact]
    public async Task PresetStore_SkipsParseableInvalidLocalJsonFiles()
    {
        var root = Path.Combine(Path.GetTempPath(), $"waller-native-tests-{Guid.NewGuid():N}");
        try
        {
            var store = new PresetStore(root);
            var saved = await store.SaveAsync(CreatePreset([
                new PresetAssignment(
                    new MonitorIdentity("DISPLAY-1", "Monitor 1", 1, 1920, 1080, 0, 0),
                    WallpaperSource.Empty,
                    WallpaperPlacement.Default),
            ]));
            var invalidId = Guid.NewGuid();
            var presetsDirectory = Path.Combine(root, "presets");
            await File.WriteAllTextAsync(Path.Combine(presetsDirectory, $"{invalidId:N}.json"), "{}");

            var listed = await store.ListAsync();
            var invalid = await store.LoadAsync(invalidId);

            Assert.Single(listed);
            Assert.Equal(saved.Id, listed[0].Id);
            Assert.Null(invalid);
        }
        finally
        {
            if (Directory.Exists(root))
            {
                Directory.Delete(root, recursive: true);
            }
        }
    }

    [Fact]
    public async Task PresetStore_SkipsUnsupportedSchemaVersions()
    {
        var root = Path.Combine(Path.GetTempPath(), $"waller-native-tests-{Guid.NewGuid():N}");
        try
        {
            var store = new PresetStore(root);
            var saved = await store.SaveAsync(CreatePreset([
                new PresetAssignment(
                    new MonitorIdentity("DISPLAY-1", "Monitor 1", 1, 1920, 1080, 0, 0),
                    WallpaperSource.Empty,
                    WallpaperPlacement.Default),
            ]));
            var unsupportedId = Guid.NewGuid();
            var presetsDirectory = Path.Combine(root, "presets");
            await File.WriteAllTextAsync(
                Path.Combine(presetsDirectory, $"{unsupportedId:N}.json"),
                $$"""
                {
                  "schemaVersion": 999,
                  "id": "{{unsupportedId}}",
                  "name": "Future",
                  "assignments": [],
                  "createdAt": "2026-01-01T00:00:00+00:00",
                  "updatedAt": "2026-01-01T00:00:00+00:00"
                }
                """);

            var listed = await store.ListAsync();
            var unsupported = await store.LoadAsync(unsupportedId);

            Assert.Single(listed);
            Assert.Equal(saved.Id, listed[0].Id);
            Assert.Null(unsupported);
        }
        finally
        {
            if (Directory.Exists(root))
            {
                Directory.Delete(root, recursive: true);
            }
        }
    }

    [Fact]
    public async Task PresetStore_SkipsLocalJsonWithInvalidAssignments()
    {
        var root = Path.Combine(Path.GetTempPath(), $"waller-native-tests-{Guid.NewGuid():N}");
        try
        {
            var store = new PresetStore(root);
            var saved = await store.SaveAsync(CreatePreset([
                new PresetAssignment(
                    new MonitorIdentity("DISPLAY-1", "Monitor 1", 1, 1920, 1080, 0, 0),
                    WallpaperSource.Empty,
                    WallpaperPlacement.Default),
            ]));
            var invalidId = Guid.NewGuid();
            var presetsDirectory = Path.Combine(root, "presets");
            await File.WriteAllTextAsync(
                Path.Combine(presetsDirectory, $"{invalidId:N}.json"),
                $$"""
                {
                  "schemaVersion": 1,
                  "id": "{{invalidId}}",
                  "name": "Invalid assignment",
                  "assignments": [null],
                  "createdAt": "2026-01-01T00:00:00+00:00",
                  "updatedAt": "2026-01-01T00:00:00+00:00"
                }
                """);

            var listed = await store.ListAsync();
            var invalid = await store.LoadAsync(invalidId);

            Assert.Single(listed);
            Assert.Equal(saved.Id, listed[0].Id);
            Assert.Null(invalid);
        }
        finally
        {
            if (Directory.Exists(root))
            {
                Directory.Delete(root, recursive: true);
            }
        }
    }

    [Fact]
    public async Task PresetStore_SkipsLocalJsonWithInvalidAssignmentSource()
    {
        var root = Path.Combine(Path.GetTempPath(), $"waller-native-tests-{Guid.NewGuid():N}");
        try
        {
            var store = new PresetStore(root);
            var saved = await store.SaveAsync(CreatePreset([
                new PresetAssignment(
                    new MonitorIdentity("DISPLAY-1", "Monitor 1", 1, 1920, 1080, 0, 0),
                    WallpaperSource.Empty,
                    WallpaperPlacement.Default),
            ]));
            var invalidId = Guid.NewGuid();
            var presetsDirectory = Path.Combine(root, "presets");
            await File.WriteAllTextAsync(
                Path.Combine(presetsDirectory, $"{invalidId:N}.json"),
                $$"""
                {
                  "schemaVersion": 1,
                  "id": "{{invalidId}}",
                  "name": "Invalid source",
                  "assignments": [
                    {
                      "savedMonitor": {
                        "monitorKey": "DISPLAY-2",
                        "deviceName": "Monitor 2",
                        "displayIndex": 2,
                        "width": 1920,
                        "height": 1080,
                        "x": 0,
                        "y": 0
                      },
                      "source": {
                        "kind": 1,
                        "colorHex": "not-a-color"
                      },
                      "placement": {
                        "fitMode": 0,
                        "anchor": 4,
                        "offsetXPercent": 0,
                        "offsetYPercent": 0
                      }
                    }
                  ],
                  "createdAt": "2026-01-01T00:00:00+00:00",
                  "updatedAt": "2026-01-01T00:00:00+00:00"
                }
                """);

            var listed = await store.ListAsync();
            var invalid = await store.LoadAsync(invalidId);

            Assert.Single(listed);
            Assert.Equal(saved.Id, listed[0].Id);
            Assert.Null(invalid);
        }
        finally
        {
            if (Directory.Exists(root))
            {
                Directory.Delete(root, recursive: true);
            }
        }
    }

    [Fact]
    public async Task PresetStore_SkipsLocalJsonWithInvalidAssignmentMonitor()
    {
        var root = Path.Combine(Path.GetTempPath(), $"waller-native-tests-{Guid.NewGuid():N}");
        try
        {
            var store = new PresetStore(root);
            var saved = await store.SaveAsync(CreatePreset([
                new PresetAssignment(
                    new MonitorIdentity("DISPLAY-1", "Monitor 1", 1, 1920, 1080, 0, 0),
                    WallpaperSource.Empty,
                    WallpaperPlacement.Default),
            ]));
            var invalidId = Guid.NewGuid();
            var presetsDirectory = Path.Combine(root, "presets");
            await File.WriteAllTextAsync(
                Path.Combine(presetsDirectory, $"{invalidId:N}.json"),
                $$"""
                {
                  "schemaVersion": 1,
                  "id": "{{invalidId}}",
                  "name": "Invalid monitor",
                  "assignments": [
                    {
                      "savedMonitor": {
                        "monitorKey": "DISPLAY-2",
                        "deviceName": "Monitor 2",
                        "displayIndex": 2,
                        "width": 0,
                        "height": 1080,
                        "x": 0,
                        "y": 0
                      },
                      "source": {
                        "kind": 2
                      },
                      "placement": {
                        "fitMode": 0,
                        "anchor": 4,
                        "offsetXPercent": 0,
                        "offsetYPercent": 0
                      }
                    }
                  ],
                  "createdAt": "2026-01-01T00:00:00+00:00",
                  "updatedAt": "2026-01-01T00:00:00+00:00"
                }
                """);

            var listed = await store.ListAsync();
            var invalid = await store.LoadAsync(invalidId);

            Assert.Single(listed);
            Assert.Equal(saved.Id, listed[0].Id);
            Assert.Null(invalid);
        }
        finally
        {
            if (Directory.Exists(root))
            {
                Directory.Delete(root, recursive: true);
            }
        }
    }

    [Fact]
    public async Task PresetStore_NormalizesMissingOrRegressingTimestampsWhenLoading()
    {
        var root = Path.Combine(Path.GetTempPath(), $"waller-native-tests-{Guid.NewGuid():N}");
        try
        {
            var store = new PresetStore(root);
            var missingTimestampId = Guid.NewGuid();
            var regressingTimestampId = Guid.NewGuid();
            var presetsDirectory = Path.Combine(root, "presets");
            Directory.CreateDirectory(presetsDirectory);
            await File.WriteAllTextAsync(
                Path.Combine(presetsDirectory, $"{missingTimestampId:N}.json"),
                $$"""
                {
                  "schemaVersion": 1,
                  "id": "{{missingTimestampId}}",
                  "name": "Missing timestamps",
                  "assignments": []
                }
                """);
            await File.WriteAllTextAsync(
                Path.Combine(presetsDirectory, $"{regressingTimestampId:N}.json"),
                $$"""
                {
                  "schemaVersion": 1,
                  "id": "{{regressingTimestampId}}",
                  "name": "Regressing timestamps",
                  "assignments": [],
                  "createdAt": "2026-06-08T12:00:00+00:00",
                  "updatedAt": "2026-06-07T12:00:00+00:00"
                }
                """);

            var missingTimestamps = await store.LoadAsync(missingTimestampId);
            var regressingTimestamps = await store.LoadAsync(regressingTimestampId);

            Assert.Equal(DateTimeOffset.UnixEpoch, missingTimestamps?.CreatedAt);
            Assert.Equal(DateTimeOffset.UnixEpoch, missingTimestamps?.UpdatedAt);
            Assert.Equal(regressingTimestamps?.CreatedAt, regressingTimestamps?.UpdatedAt);
        }
        finally
        {
            if (Directory.Exists(root))
            {
                Directory.Delete(root, recursive: true);
            }
        }
    }

    [Fact]
    public void MonitorIdentity_ReportsWhetherItCanBeUsedInPresetAssignments()
    {
        Assert.True(new MonitorIdentity("DISPLAY-1", null, 1, 1920, 1080, 0, 0).IsValidForPresetAssignment);
        Assert.False(new MonitorIdentity(" ", null, 1, 1920, 1080, 0, 0).IsValidForPresetAssignment);
        Assert.False(new MonitorIdentity("DISPLAY-1", null, 1, 0, 1080, 0, 0).IsValidForPresetAssignment);
        Assert.False(new MonitorIdentity("DISPLAY-1", null, 1, 1920, 0, 0, 0).IsValidForPresetAssignment);
    }

    [Fact]
    public void WallpaperSource_TryNormalizeValidatesPayload()
    {
        Assert.Equal(
            WallpaperSource.Empty,
            WallpaperSource.TryNormalize(new WallpaperSource(WallpaperSourceKind.Empty, "relative\\ignored.png")));
        Assert.Equal(
            "#aabbcc",
            WallpaperSource.TryNormalize(new WallpaperSource(WallpaperSourceKind.SolidColor, ColorHex: "AABBCC"))?.ColorHex);
        Assert.True(WallpaperSource.TryNormalize(
            new WallpaperSource(WallpaperSourceKind.Image, @"C:\Wallpapers\legacy.png")) is { Kind: WallpaperSourceKind.Image });
        Assert.Null(WallpaperSource.TryNormalize(new WallpaperSource(WallpaperSourceKind.Image, "relative\\wallpaper.png")));
        Assert.Null(WallpaperSource.TryNormalize(new WallpaperSource(WallpaperSourceKind.SolidColor, ColorHex: "bad")));
    }

    [Fact]
    public async Task PresetStore_SkipsLockedLocalJsonFiles()
    {
        var root = Path.Combine(Path.GetTempPath(), $"waller-native-tests-{Guid.NewGuid():N}");
        FileStream? lockedStream = null;

        try
        {
            var store = new PresetStore(root);
            var saved = await store.SaveAsync(CreatePreset([
                new PresetAssignment(
                    new MonitorIdentity("DISPLAY-1", "Monitor 1", 1, 1920, 1080, 0, 0),
                    WallpaperSource.Empty,
                    WallpaperPlacement.Default),
            ]));
            var lockedId = Guid.NewGuid();
            var lockedPath = Path.Combine(root, "presets", $"{lockedId:N}.json");
            await File.WriteAllTextAsync(lockedPath, "{}");
            lockedStream = new FileStream(lockedPath, FileMode.Open, FileAccess.ReadWrite, FileShare.None);

            var listed = await store.ListAsync();
            var lockedPreset = await store.LoadAsync(lockedId);

            Assert.Single(listed);
            Assert.Equal(saved.Id, listed[0].Id);
            Assert.Null(lockedPreset);
        }
        finally
        {
            lockedStream?.Dispose();
            if (Directory.Exists(root))
            {
                Directory.Delete(root, recursive: true);
            }
        }
    }

    [Fact]
    public async Task PresetFactory_CreatesPresetFromSessionIncludingMissingAssignments()
    {
        var session = await new ActiveSessionFactory(new SampleMonitorDetector())
            .CreateFromCurrentWindowsStateAsync();
        var missing = new PresetAssignment(
            new MonitorIdentity("MISSING", "Missing", 4, 1920, 1080, 0, 0),
            WallpaperSource.Empty,
            WallpaperPlacement.Default);
        session = session with { MissingAssignments = [missing] };

        var preset = PresetFactory.CreateFromSession(session, "Desk");

        Assert.Equal("Desk", preset.Name);
        Assert.Equal(session.Monitors.Count + 1, preset.Assignments.Count);
        Assert.Contains(preset.Assignments, assignment => assignment.SavedMonitor.MonitorKey == "MISSING");
    }

    [Fact]
    public void PresetFactory_DuplicatesPresetWithNewIdentityAndName()
    {
        var preset = CreatePreset([]) with { CreatedAt = DateTimeOffset.UnixEpoch };

        var duplicate = PresetFactory.Duplicate(preset, "Copy");

        Assert.NotEqual(preset.Id, duplicate.Id);
        Assert.Equal("Copy", duplicate.Name);
        Assert.Equal(preset.Assignments, duplicate.Assignments);
        Assert.True(duplicate.CreatedAt > preset.CreatedAt);
        Assert.Equal(duplicate.CreatedAt, duplicate.UpdatedAt);
    }

    [Fact]
    public void PresetFactory_RenamesPresetWithoutChangingIdentity()
    {
        var preset = CreatePreset([]);

        var renamed = PresetFactory.Rename(preset, "Renamed");

        Assert.Equal(preset.Id, renamed.Id);
        Assert.Equal("Renamed", renamed.Name);
        Assert.Equal(preset.Assignments, renamed.Assignments);
        Assert.Equal(preset.CreatedAt, renamed.CreatedAt);
        Assert.Equal(preset.UpdatedAt, renamed.UpdatedAt);
    }

    [Fact]
    public async Task PresetFactory_PreservesAndClampsPlacementOffsets()
    {
        var session = await new ActiveSessionFactory(new SampleMonitorDetector())
            .CreateFromCurrentWindowsStateAsync();
        var editor = new ActiveSessionEditor();
        var monitorKey = session.Monitors[0].Monitor.Identity.MonitorKey;
        session = editor.UpdateAssignment(
            session,
            monitorKey,
            WallpaperSource.FromSolidColor("#112233"),
            new WallpaperPlacement(WallpaperFitMode.Cover, WallpaperAnchor.Center, 80, -120));
        var missing = new PresetAssignment(
            new MonitorIdentity("MISSING", "Missing", 4, 1920, 1080, 0, 0),
            WallpaperSource.Empty,
            new WallpaperPlacement(WallpaperFitMode.Contain, WallpaperAnchor.Bottom, -140, 40));
        session = session with { MissingAssignments = [missing] };

        var preset = PresetFactory.CreateFromSession(session, "Desk");
        var currentAssignment = preset.Assignments.Single(assignment => assignment.SavedMonitor.MonitorKey == monitorKey);
        var missingAssignment = preset.Assignments.Single(assignment => assignment.SavedMonitor.MonitorKey == "MISSING");

        Assert.Equal(80, currentAssignment.Placement.OffsetXPercent);
        Assert.Equal(-100, currentAssignment.Placement.OffsetYPercent);
        Assert.Equal(-100, missingAssignment.Placement.OffsetXPercent);
        Assert.Equal(40, missingAssignment.Placement.OffsetYPercent);
    }

    [Fact]
    public async Task PresetFactory_NormalizesDuplicateAssignmentsCaseInsensitively()
    {
        var session = await new ActiveSessionFactory(new SampleMonitorDetector())
            .CreateFromCurrentWindowsStateAsync();
        var editor = new ActiveSessionEditor();
        var monitor = session.Monitors[0].Monitor.Identity;
        var monitorKey = monitor.MonitorKey;
        session = editor.UpdateAssignment(
            session,
            monitorKey,
            WallpaperSource.FromSolidColor("#112233"),
            WallpaperPlacement.Default);
        var duplicateMissing = new PresetAssignment(
            monitor with { MonitorKey = monitorKey.ToLowerInvariant() },
            WallpaperSource.FromSolidColor("#445566"),
            new WallpaperPlacement(WallpaperFitMode.Stretch, WallpaperAnchor.Bottom));
        session = session with { MissingAssignments = [duplicateMissing] };

        var preset = PresetFactory.CreateFromSession(session, "Desk");
        var matchingAssignments = preset.Assignments
            .Where(assignment => MonitorKeys.Equals(assignment.SavedMonitor.MonitorKey, monitorKey))
            .ToList();

        Assert.Single(matchingAssignments);
        Assert.Equal("#112233", matchingAssignments[0].Source.ColorHex);
        Assert.DoesNotContain(preset.Assignments, assignment => assignment.Source.ColorHex == "#445566");
    }

    [Fact]
    public async Task UserSettingsStore_RoundTripsSettings()
    {
        var root = Path.Combine(Path.GetTempPath(), $"waller-settings-tests-{Guid.NewGuid():N}");
        try
        {
            var store = new UserSettingsStore(root);
            var settings = UserSettings.Default with
            {
                Theme = AppThemePreference.Dark,
                Language = "es",
                LastSelectedPresetId = Guid.NewGuid(),
            };

            await store.SaveAsync(settings);
            var loaded = await store.LoadAsync();

            Assert.Equal(AppThemePreference.Dark, loaded.Theme);
            Assert.Equal("es", loaded.Language);
            Assert.Equal(settings.LastSelectedPresetId, loaded.LastSelectedPresetId);
        }
        finally
        {
            if (Directory.Exists(root))
            {
                Directory.Delete(root, recursive: true);
            }
        }
    }

    [Fact]
    public async Task UserSettingsStore_KeepsExistingJsonWhenAtomicSaveCannotReplaceFile()
    {
        var root = Path.Combine(Path.GetTempPath(), $"waller-settings-tests-{Guid.NewGuid():N}");
        FileStream? lockedStream = null;

        try
        {
            var store = new UserSettingsStore(root);
            await store.SaveAsync(UserSettings.Default with
            {
                Theme = AppThemePreference.Dark,
                Language = "es",
            });
            lockedStream = new FileStream(
                Path.Combine(root, "settings.json"),
                FileMode.Open,
                FileAccess.ReadWrite,
                FileShare.None);

            var error = await Record.ExceptionAsync(() => store.SaveAsync(UserSettings.Default with
            {
                Theme = AppThemePreference.Light,
                Language = "en",
            }));

            Assert.True(error is IOException or UnauthorizedAccessException);
            lockedStream.Dispose();
            lockedStream = null;

            var loaded = await store.LoadAsync();
            var tempFiles = Directory.EnumerateFiles(root, "*.tmp").ToList();

            Assert.Equal(AppThemePreference.Dark, loaded.Theme);
            Assert.Equal("es", loaded.Language);
            Assert.Empty(tempFiles);
        }
        finally
        {
            lockedStream?.Dispose();
            if (Directory.Exists(root))
            {
                Directory.Delete(root, recursive: true);
            }
        }
    }

    [Fact]
    public async Task UserSettingsStore_FallsBackWhenLocalJsonIsCorrupt()
    {
        var root = Path.Combine(Path.GetTempPath(), $"waller-settings-tests-{Guid.NewGuid():N}");
        try
        {
            Directory.CreateDirectory(root);
            await File.WriteAllTextAsync(Path.Combine(root, "settings.json"), "{ broken");
            var store = new UserSettingsStore(root);

            var loaded = await store.LoadAsync();

            Assert.Equal(UserSettings.Default, loaded);
        }
        finally
        {
            if (Directory.Exists(root))
            {
                Directory.Delete(root, recursive: true);
            }
        }
    }

    [Fact]
    public async Task UserSettingsStore_FallsBackWhenLocalJsonIsLocked()
    {
        var root = Path.Combine(Path.GetTempPath(), $"waller-settings-tests-{Guid.NewGuid():N}");
        FileStream? lockedStream = null;

        try
        {
            var store = new UserSettingsStore(root);
            await store.SaveAsync(UserSettings.Default with
            {
                Theme = AppThemePreference.Dark,
                Language = "es",
            });
            lockedStream = new FileStream(
                Path.Combine(root, "settings.json"),
                FileMode.Open,
                FileAccess.ReadWrite,
                FileShare.None);

            var loaded = await store.LoadAsync();

            Assert.Equal(UserSettings.Default, loaded);
        }
        finally
        {
            lockedStream?.Dispose();
            if (Directory.Exists(root))
            {
                Directory.Delete(root, recursive: true);
            }
        }
    }

    [Fact]
    public async Task UserSettingsStore_NormalizesUnsupportedLocalValues()
    {
        var root = Path.Combine(Path.GetTempPath(), $"waller-settings-tests-{Guid.NewGuid():N}");
        try
        {
            var store = new UserSettingsStore(root);
            await store.SaveAsync(UserSettings.Default with
            {
                Theme = (AppThemePreference)999,
                Language = "fr",
                WindowWidth = 10,
                WindowHeight = 20,
            });

            var loaded = await store.LoadAsync();

            Assert.Equal(AppThemePreference.System, loaded.Theme);
            Assert.Equal(AppLanguages.English, loaded.Language);
            Assert.Equal(UserSettingsPolicy.MinWindowWidth, loaded.WindowWidth);
            Assert.Equal(UserSettingsPolicy.MinWindowHeight, loaded.WindowHeight);
        }
        finally
        {
            if (Directory.Exists(root))
            {
                Directory.Delete(root, recursive: true);
            }
        }
    }

    [Fact]
    public async Task UserSettingsStore_SavesCanonicalLanguageCode()
    {
        var root = Path.Combine(Path.GetTempPath(), $"waller-settings-tests-{Guid.NewGuid():N}");
        try
        {
            var store = new UserSettingsStore(root);
            await store.SaveAsync(UserSettings.Default with { Language = "ES" });

            var loaded = await store.LoadAsync();

            Assert.Equal("es", loaded.Language);
        }
        finally
        {
            if (Directory.Exists(root))
            {
                Directory.Delete(root, recursive: true);
            }
        }
    }

    [Fact]
    public async Task UserSettingsStore_DropsIncompleteWindowPosition()
    {
        var root = Path.Combine(Path.GetTempPath(), $"waller-settings-tests-{Guid.NewGuid():N}");
        try
        {
            var store = new UserSettingsStore(root);
            await store.SaveAsync(UserSettings.Default with
            {
                WindowX = 120,
                WindowY = null,
            });

            var loaded = await store.LoadAsync();

            Assert.Null(loaded.WindowX);
            Assert.Null(loaded.WindowY);
        }
        finally
        {
            if (Directory.Exists(root))
            {
                Directory.Delete(root, recursive: true);
            }
        }
    }

    [Fact]
    public void UserSettingsPolicy_NormalizesThemeLanguageAndWindowPlacement()
    {
        var normalized = UserSettingsPolicy.Normalize(UserSettings.Default with
        {
            Theme = (AppThemePreference)999,
            Language = "ES",
            WindowWidth = 1,
            WindowHeight = 2,
            WindowX = 100,
            WindowY = null,
        });

        Assert.Equal(UserSettings.Default.Theme, normalized.Theme);
        Assert.Equal(AppLanguages.Spanish, normalized.Language);
        Assert.Equal(UserSettingsPolicy.MinWindowWidth, normalized.WindowWidth);
        Assert.Equal(UserSettingsPolicy.MinWindowHeight, normalized.WindowHeight);
        Assert.Null(normalized.WindowX);
        Assert.Null(normalized.WindowY);
    }

    [Fact]
    public void UserSettings_WithWindowPlacementSetsCompletePlacement()
    {
        var updated = UserSettings.Default.WithWindowPlacement(1280, 720, -20, 40);

        Assert.Equal(1280, updated.WindowWidth);
        Assert.Equal(720, updated.WindowHeight);
        Assert.Equal(-20, updated.WindowX);
        Assert.Equal(40, updated.WindowY);
    }

    [Fact]
    public void UserSettings_WithPreferencesPreservesWindowPlacement()
    {
        var presetId = Guid.NewGuid();
        var current = UserSettings.Default.WithWindowPlacement(1280, 720, -20, 40);

        var updated = current.WithPreferences(
            AppThemePreference.Dark,
            AppLanguages.Spanish,
            presetId);

        Assert.Equal(AppThemePreference.Dark, updated.Theme);
        Assert.Equal(AppLanguages.Spanish, updated.Language);
        Assert.Equal(presetId, updated.LastSelectedPresetId);
        Assert.Equal(1280, updated.WindowWidth);
        Assert.Equal(720, updated.WindowHeight);
        Assert.Equal(-20, updated.WindowX);
        Assert.Equal(40, updated.WindowY);
    }

    [Fact]
    public void UserSettings_WithLastSelectedPresetPreservesPreferencesAndWindowPlacement()
    {
        var presetId = Guid.NewGuid();
        var current = UserSettings.Default
            .WithWindowPlacement(1280, 720, -20, 40)
            .WithPreferences(AppThemePreference.Dark, AppLanguages.Spanish, null);

        var updated = current.WithLastSelectedPreset(presetId);

        Assert.Equal(AppThemePreference.Dark, updated.Theme);
        Assert.Equal(AppLanguages.Spanish, updated.Language);
        Assert.Equal(presetId, updated.LastSelectedPresetId);
        Assert.Equal(1280, updated.WindowWidth);
        Assert.Equal(720, updated.WindowHeight);
        Assert.Equal(-20, updated.WindowX);
        Assert.Equal(40, updated.WindowY);
    }

    [Fact]
    public async Task RenderedWallpaperStore_ClearDeletesRenderedPngFiles()
    {
        var root = Path.Combine(Path.GetTempPath(), $"waller-render-cache-tests-{Guid.NewGuid():N}");
        try
        {
            var store = new RenderedWallpaperStore(root);
            var path = store.CreatePath("DISPLAY-1");
            await File.WriteAllBytesAsync(path, [1, 2, 3]);

            var result = store.Clear();

            Assert.Equal(1, result.Deleted);
            Assert.Equal(0, result.Failed);
            Assert.False(result.HasFailures);
            Assert.False(File.Exists(path));
        }
        finally
        {
            if (Directory.Exists(root))
            {
                Directory.Delete(root, recursive: true);
            }
        }
    }

    [Fact]
    public async Task RenderedWallpaperStore_ClearKeepsNonPngFiles()
    {
        var root = Path.Combine(Path.GetTempPath(), $"waller-render-cache-tests-{Guid.NewGuid():N}");
        try
        {
            var store = new RenderedWallpaperStore(root);
            Directory.CreateDirectory(store.RenderedDirectory);
            var keepPath = Path.Combine(store.RenderedDirectory, "keep.txt");
            await File.WriteAllTextAsync(keepPath, "keep");

            var result = store.Clear();

            Assert.Equal(0, result.Deleted);
            Assert.Equal(0, result.Failed);
            Assert.False(result.HasFailures);
            Assert.True(File.Exists(keepPath));
        }
        finally
        {
            if (Directory.Exists(root))
            {
                Directory.Delete(root, recursive: true);
            }
        }
    }

    [Fact]
    public async Task RenderedWallpaperStore_ClearDeletesRenderedTempFiles()
    {
        var root = Path.Combine(Path.GetTempPath(), $"waller-render-cache-tests-{Guid.NewGuid():N}");
        try
        {
            var store = new RenderedWallpaperStore(root);
            Directory.CreateDirectory(store.RenderedDirectory);
            var tempPath = Path.Combine(store.RenderedDirectory, ".wallpaper.png.123.tmp");
            await File.WriteAllBytesAsync(tempPath, [1, 2, 3]);

            var result = store.Clear();

            Assert.Equal(1, result.Deleted);
            Assert.Equal(0, result.Failed);
            Assert.False(File.Exists(tempPath));
        }
        finally
        {
            if (Directory.Exists(root))
            {
                Directory.Delete(root, recursive: true);
            }
        }
    }

    [Fact]
    public async Task RenderedWallpaperStore_ClearKeepsUnrelatedTempFiles()
    {
        var root = Path.Combine(Path.GetTempPath(), $"waller-render-cache-tests-{Guid.NewGuid():N}");
        try
        {
            var store = new RenderedWallpaperStore(root);
            Directory.CreateDirectory(store.RenderedDirectory);
            var keepPath = Path.Combine(store.RenderedDirectory, "notes.tmp");
            await File.WriteAllTextAsync(keepPath, "keep");

            var result = store.Clear();

            Assert.Equal(0, result.Deleted);
            Assert.Equal(0, result.Failed);
            Assert.False(result.HasFailures);
            Assert.True(File.Exists(keepPath));
        }
        finally
        {
            if (Directory.Exists(root))
            {
                Directory.Delete(root, recursive: true);
            }
        }
    }

    [Fact]
    public async Task RenderedWallpaperStore_ClearReportsFailureWhenRenderedPathIsFile()
    {
        var root = Path.Combine(Path.GetTempPath(), $"waller-render-cache-tests-{Guid.NewGuid():N}");
        try
        {
            var store = new RenderedWallpaperStore(root);
            Directory.CreateDirectory(root);
            await File.WriteAllTextAsync(store.RenderedDirectory, "blocked");

            var result = store.Clear();

            Assert.Equal(0, result.Deleted);
            Assert.Equal(1, result.Failed);
            Assert.True(result.HasFailures);
        }
        finally
        {
            if (Directory.Exists(root))
            {
                Directory.Delete(root, recursive: true);
            }
        }
    }

    [Fact]
    public void RenderedWallpaperStore_ClearFilesReportsEnumerationFailure()
    {
        static IEnumerable<string> ThrowingFiles()
        {
            yield return @"C:\cache\notes.txt";
            throw new IOException("enumeration blocked");
        }

        var result = RenderedWallpaperStore.ClearFiles(ThrowingFiles());

        Assert.Equal(0, result.Deleted);
        Assert.Equal(1, result.Failed);
        Assert.True(result.HasFailures);
    }

    [Fact]
    public void RenderedWallpaperFileNames_IdentifiesCacheFiles()
    {
        Assert.True(RenderedWallpaperFileNames.IsCacheFile(@"C:\cache\wallpaper.png"));
        Assert.True(RenderedWallpaperFileNames.IsCacheFile(@"C:\cache\.wallpaper.png.123.tmp"));
        Assert.False(RenderedWallpaperFileNames.IsCacheFile(@"C:\cache\notes.tmp"));
        Assert.False(RenderedWallpaperFileNames.IsCacheFile(@"C:\cache\notes.txt"));
    }

    [Fact]
    public void RenderedCacheClearResult_ReportsFailures()
    {
        Assert.False(new RenderedCacheClearResult(Deleted: 2, Failed: 0).HasFailures);
        Assert.True(new RenderedCacheClearResult(Deleted: 2, Failed: 1).HasFailures);
    }

    [Fact]
    public void RenderedWallpaperStore_SanitizesRenderedFileNames()
    {
        var root = Path.Combine(Path.GetTempPath(), $"waller-render-cache-tests-{Guid.NewGuid():N}");
        try
        {
            var store = new RenderedWallpaperStore(root);
            var path = store.CreatePath(@"\\?\DISPLAY#A:B*C?D|E<FGH>");
            var fileName = Path.GetFileName(path);

            Assert.Equal(store.RenderedDirectory, Path.GetDirectoryName(path));
            Assert.Equal(-1, fileName.IndexOfAny(Path.GetInvalidFileNameChars()));
            Assert.EndsWith(".png", fileName);
        }
        finally
        {
            if (Directory.Exists(root))
            {
                Directory.Delete(root, recursive: true);
            }
        }
    }

    [Fact]
    public void RenderedWallpaperStore_UsesHashToAvoidSanitizedNameCollisions()
    {
        var first = RenderedWallpaperFileNames.Create("DISPLAY:A", DateTimeOffset.UnixEpoch);
        var second = RenderedWallpaperFileNames.Create("DISPLAY?A", DateTimeOffset.UnixEpoch);

        Assert.NotEqual(first, second);
        Assert.StartsWith("DISPLAY_A_", first);
        Assert.StartsWith("DISPLAY_A_", second);
    }

    [Fact]
    public void RenderedWallpaperStore_BoundsRenderedFileNameLength()
    {
        var monitorKey = new string('A', 300);

        var fileName = RenderedWallpaperFileNames.Create(monitorKey, DateTimeOffset.UnixEpoch);

        Assert.True(fileName.Length < 96);
        Assert.StartsWith(new string('A', 48), fileName);
    }

    [Fact]
    public async Task AtomicFileWriter_WritesNewFileAfterCallbackCompletes()
    {
        var root = Path.Combine(Path.GetTempPath(), $"waller-atomic-write-tests-{Guid.NewGuid():N}");
        try
        {
            var path = Path.Combine(root, "data.bin");
            byte[] bytes = [1, 2, 3, 4];

            await AtomicFileWriter.WriteAsync(
                path,
                async (stream, token) => await stream.WriteAsync(bytes, token),
                CancellationToken.None);

            Assert.Equal(bytes, await File.ReadAllBytesAsync(path));
            Assert.Empty(Directory.EnumerateFiles(root, "*.tmp"));
        }
        finally
        {
            if (Directory.Exists(root))
            {
                Directory.Delete(root, recursive: true);
            }
        }
    }

    [Fact]
    public async Task AtomicFileWriter_KeepsExistingFileWhenWriteFails()
    {
        var root = Path.Combine(Path.GetTempPath(), $"waller-atomic-write-tests-{Guid.NewGuid():N}");
        try
        {
            Directory.CreateDirectory(root);
            var path = Path.Combine(root, "data.bin");
            await File.WriteAllBytesAsync(path, [9, 8, 7]);

            var error = await Record.ExceptionAsync(() => AtomicFileWriter.WriteAsync(
                path,
                (_, _) => throw new IOException("boom"),
                CancellationToken.None));

            Assert.IsType<IOException>(error);
            Assert.Equal([9, 8, 7], await File.ReadAllBytesAsync(path));
            Assert.Empty(Directory.EnumerateFiles(root, "*.tmp"));
        }
        finally
        {
            if (Directory.Exists(root))
            {
                Directory.Delete(root, recursive: true);
            }
        }
    }

    [Fact]
    public async Task SolidColorPngWriter_KeepsExistingFileWhenAtomicWriteIsCancelled()
    {
        var root = Path.Combine(Path.GetTempPath(), $"waller-render-cache-tests-{Guid.NewGuid():N}");
        try
        {
            Directory.CreateDirectory(root);
            var path = Path.Combine(root, "wallpaper.png");
            await File.WriteAllBytesAsync(path, [9, 8, 7]);
            var pixels = PixelBuffer.CreateSolid(2, 2, RgbColor.Black);
            using var cts = new CancellationTokenSource();
            cts.Cancel();

            var error = await Record.ExceptionAsync(() => SolidColorPngWriter.WriteAsync(path, pixels, cts.Token));

            Assert.IsAssignableFrom<OperationCanceledException>(error);
            Assert.Equal([9, 8, 7], await File.ReadAllBytesAsync(path));
            Assert.Empty(Directory.EnumerateFiles(root, "*.tmp"));
        }
        finally
        {
            if (Directory.Exists(root))
            {
                Directory.Delete(root, recursive: true);
            }
        }
    }

    [Fact]
    public async Task SolidColorPngWriter_WritesCompletePngThroughAtomicPath()
    {
        var root = Path.Combine(Path.GetTempPath(), $"waller-render-cache-tests-{Guid.NewGuid():N}");
        try
        {
            Directory.CreateDirectory(root);
            var path = Path.Combine(root, "wallpaper.png");
            var pixels = PixelBuffer.CreateSolid(3, 2, new RgbColor(1, 2, 3));

            await SolidColorPngWriter.WriteAsync(path, pixels);
            var size = ReadPngSize(path);

            Assert.Equal((3, 2), size);
            Assert.Empty(Directory.EnumerateFiles(root, "*.tmp"));
        }
        finally
        {
            if (Directory.Exists(root))
            {
                Directory.Delete(root, recursive: true);
            }
        }
    }

    [Fact]
    public void PixelBuffer_RejectsInvalidDimensions()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new PixelBuffer(0, 1, []));
        Assert.Throws<ArgumentOutOfRangeException>(() => new PixelBuffer(1, 0, []));
    }

    [Fact]
    public void PixelBuffer_RejectsInvalidDataLength()
    {
        var error = Assert.Throws<ArgumentException>(() => new PixelBuffer(2, 2, new byte[3]));

        Assert.Equal("data", error.ParamName);
    }

    [Fact]
    public void DesktopWallpaperInterop_MapsEmptyWallpaperPathToEmptySource()
    {
        var source = DesktopWallpaperInterop.WallpaperPathToSource("   ");

        Assert.Equal(WallpaperSourceKind.Empty, source.Kind);
    }

    [Fact]
    public void DesktopWallpaperInterop_MapsWallpaperPathToImageSource()
    {
        var source = DesktopWallpaperInterop.WallpaperPathToSource(@" C:\Wallpapers\current.jpg ");

        Assert.Equal(WallpaperSourceKind.Image, source.Kind);
        Assert.Equal(@"C:\Wallpapers\current.jpg", source.ImagePath);
    }

    [Fact]
    public void DesktopWallpaperInterop_MapsInvalidWallpaperPathToEmptySource()
    {
        var source = DesktopWallpaperInterop.WallpaperPathToSource("relative\\wallpaper.jpg");

        Assert.Equal(WallpaperSourceKind.Empty, source.Kind);
    }

    [Fact]
    public void DesktopWallpaperInterop_MapsWindowsBackgroundColorToSolidSource()
    {
        var source = DesktopWallpaperInterop.BackgroundColorResultToSource(0, 0x00332211);

        Assert.Equal(WallpaperSourceKind.SolidColor, source.Kind);
        Assert.Equal("#112233", source.ColorHex);
    }

    [Fact]
    public void DesktopWallpaperInterop_FallsBackWhenWindowsBackgroundColorReadFails()
    {
        var source = DesktopWallpaperInterop.BackgroundColorResultToSource(
            unchecked((int)0x80004005),
            0x00332211);

        Assert.Equal(WallpaperSourceKind.Empty, source.Kind);
    }

    [Theory]
    [InlineData(DesktopWallpaperPosition.Center, WallpaperFitMode.Center)]
    [InlineData(DesktopWallpaperPosition.Tile, WallpaperFitMode.Tile)]
    [InlineData(DesktopWallpaperPosition.Stretch, WallpaperFitMode.Stretch)]
    [InlineData(DesktopWallpaperPosition.Fit, WallpaperFitMode.Contain)]
    [InlineData(DesktopWallpaperPosition.Fill, WallpaperFitMode.Cover)]
    [InlineData(DesktopWallpaperPosition.Span, WallpaperFitMode.Cover)]
    public void DesktopWallpaperInterop_MapsWindowsPositionToPlacement(
        DesktopWallpaperPosition position,
        WallpaperFitMode fitMode)
    {
        var placement = DesktopWallpaperInterop.PositionToPlacement(position);

        Assert.Equal(fitMode, placement.FitMode);
        Assert.Equal(WallpaperAnchor.Center, placement.Anchor);
    }

    [Fact]
    public void DesktopWallpaperInterop_FallsBackWhenWindowsPositionReadFails()
    {
        var placement = DesktopWallpaperInterop.PositionResultToPlacement(
            unchecked((int)0x80004005),
            DesktopWallpaperPosition.Fit);

        Assert.Equal(WallpaperPlacement.Default, placement);
    }

    [Fact]
    public void DesktopWallpaperInterop_SetsWallpaperBeforePosition()
    {
        var desktopWallpaper = new RecordingDesktopWallpaperCom();

        DesktopWallpaperInterop.SetWallpaperThenPosition(
            desktopWallpaper,
            "DISPLAY-1",
            @"C:\Wallpapers\rendered.png",
            DesktopWallpaperPosition.Fill);

        Assert.Equal(
            [
                "SetWallpaper:DISPLAY-1:C:\\Wallpapers\\rendered.png",
                "SetPosition:Fill",
            ],
            desktopWallpaper.Calls);
    }

    [Fact]
    public void WallpaperSource_NormalizesFullImagePath()
    {
        var source = WallpaperSource.FromImage(@"  C:\Wallpapers\current.jpg  ");

        Assert.Equal(@"C:\Wallpapers\current.jpg", source.ImagePath);
    }

    [Fact]
    public void WallpaperSource_RejectsRelativeImagePath()
    {
        var error = Assert.Throws<WallpaperSourcePathException>(() =>
            WallpaperSource.FromImage(@"wallpapers\current.jpg"));

        Assert.Equal(WallpaperSourcePathException.FullyQualifiedRequired, error.ErrorCode);
        Assert.Equal("imagePath", error.ParamName);
    }

    [Fact]
    public void WallpaperSource_RejectsUnsupportedImagePath()
    {
        var error = Assert.Throws<WallpaperSourcePathException>(() =>
            WallpaperSource.FromImage(@"C:\Wallpapers\current.txt"));

        Assert.Equal(WallpaperSourcePathException.UnsupportedFileType, error.ErrorCode);
        Assert.Equal("imagePath", error.ParamName);
    }

    [Fact]
    public void WallpaperSource_RejectsBlankImagePath()
    {
        var error = Assert.Throws<WallpaperSourcePathException>(() =>
            WallpaperSource.FromImage("   "));

        Assert.Equal(WallpaperSourcePathException.Required, error.ErrorCode);
        Assert.Equal("imagePath", error.ParamName);
    }

    [Fact]
    public async Task WallpaperSourceFiles_DetectsExistingAndMissingImageFiles()
    {
        var path = Path.Combine(Path.GetTempPath(), $"waller-source-file-{Guid.NewGuid():N}.png");
        try
        {
            var source = WallpaperSource.FromImage(path);
            Assert.True(WallpaperSourceFiles.IsMissingImageFile(source));
            Assert.False(WallpaperSourceFiles.HasExistingImageFile(source));

            await File.WriteAllBytesAsync(path, [1, 2, 3]);

            Assert.False(WallpaperSourceFiles.IsMissingImageFile(source));
            Assert.True(WallpaperSourceFiles.HasExistingImageFile(source));
            Assert.Equal(Path.GetFileName(path), WallpaperSourceFiles.ImageFileName(source));
        }
        finally
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
    }

    [Fact]
    public void WallpaperSourceFiles_IgnoresNonImageSources()
    {
        var source = WallpaperSource.FromSolidColor("#112233");

        Assert.False(WallpaperSourceFiles.IsMissingImageFile(source));
        Assert.False(WallpaperSourceFiles.HasExistingImageFile(source));
        Assert.Null(WallpaperSourceFiles.ImageFileName(source));
    }

    [Fact]
    public async Task DesktopWallpaperApplier_FailsBeforeInteropWhenRenderedFileIsMissing()
    {
        var applier = new DesktopWallpaperApplier();
        var monitor = new MonitorIdentity("DISPLAY-1", "Monitor 1", 1, 1920, 1080, 0, 0);
        var wallpaper = new RenderedWallpaper(
            monitor,
            Path.Combine(Path.GetTempPath(), $"missing-{Guid.NewGuid():N}.png"),
            1920,
            1080,
            DateTimeOffset.UtcNow);

        var result = await applier.ApplyAsync(wallpaper);

        Assert.False(result.Succeeded);
        Assert.Equal(ApplyErrorCodes.RenderedWallpaperMissing, result.ErrorCode);
        Assert.Contains("does not exist", result.ErrorMessage);
    }

    [Fact]
    public async Task DesktopWallpaperApplier_WritesRenderedWallpaperToMonitor()
    {
        var path = Path.Combine(Path.GetTempPath(), $"waller-applier-{Guid.NewGuid():N}.png");
        try
        {
            await File.WriteAllBytesAsync(path, [1, 2, 3]);
            var writer = new RecordingDesktopWallpaperWriter();
            var applier = new DesktopWallpaperApplier(writer);
            var monitor = new MonitorIdentity("DISPLAY-1", "Monitor 1", 1, 1920, 1080, 0, 0);
            var wallpaper = new RenderedWallpaper(monitor, path, 1920, 1080, DateTimeOffset.UtcNow);

            var result = await applier.ApplyAsync(wallpaper);

            Assert.True(result.Succeeded);
            Assert.Equal("DISPLAY-1", writer.MonitorId);
            Assert.Equal(path, writer.WallpaperPath);
            Assert.Equal(DesktopWallpaperPosition.Fill, writer.Position);
        }
        finally
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
    }

    [Fact]
    public async Task DesktopWallpaperApplier_MapsWriterFailureToFriendlyApplyFailure()
    {
        var path = Path.Combine(Path.GetTempPath(), $"waller-applier-{Guid.NewGuid():N}.png");
        try
        {
            await File.WriteAllBytesAsync(path, [1, 2, 3]);
            var applier = new DesktopWallpaperApplier(new ThrowingDesktopWallpaperWriter(
                new InvalidOperationException("COM unavailable.")));
            var monitor = new MonitorIdentity("DISPLAY-1", "Monitor 1", 1, 1920, 1080, 0, 0);
            var wallpaper = new RenderedWallpaper(monitor, path, 1920, 1080, DateTimeOffset.UtcNow);

            var result = await applier.ApplyAsync(wallpaper);

            Assert.False(result.Succeeded);
            Assert.Equal(ApplyErrorCodes.WallpaperApplyFailed, result.ErrorCode);
            Assert.Equal("COM unavailable.", result.ErrorMessage);
        }
        finally
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
    }

    [Fact]
    public void ApplyResult_SuccessClearsErrorFields()
    {
        var monitor = new MonitorIdentity("DISPLAY-1", "Monitor 1", 1, 1920, 1080, 0, 0);

        var result = ApplyResult.Success(monitor);

        Assert.True(result.Succeeded);
        Assert.Null(result.ErrorCode);
        Assert.Null(result.ErrorMessage);
    }

    [Fact]
    public void ApplyResult_FailurePreservesKnownErrorCode()
    {
        var monitor = new MonitorIdentity("DISPLAY-1", "Monitor 1", 1, 1920, 1080, 0, 0);

        var result = ApplyResult.Failure(
            monitor,
            ApplyErrorCodes.RenderedWallpaperMissing,
            "Rendered wallpaper missing.");

        Assert.False(result.Succeeded);
        Assert.Equal(ApplyErrorCodes.RenderedWallpaperMissing, result.ErrorCode);
        Assert.Equal("Rendered wallpaper missing.", result.ErrorMessage);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("driver exploded")]
    public void ApplyResult_FailureFallsBackForUnknownErrorCode(string? errorCode)
    {
        var monitor = new MonitorIdentity("DISPLAY-1", "Monitor 1", 1, 1920, 1080, 0, 0);

        var result = ApplyResult.Failure(monitor, errorCode, "Interop failed.");

        Assert.False(result.Succeeded);
        Assert.Equal(ApplyErrorCodes.WallpaperApplyFailed, result.ErrorCode);
        Assert.Equal("Interop failed.", result.ErrorMessage);
    }

    [Fact]
    public async Task WindowsMonitorDetector_ReadsWallpaperSnapshotsThroughReader()
    {
        var detector = new WindowsMonitorDetector(new FixedDesktopWallpaperReader(
            new WallpaperPlacement(WallpaperFitMode.Contain, WallpaperAnchor.Center),
            WallpaperSource.FromSolidColor("#112233"),
            [
                new DesktopWallpaperSnapshot(
                    @"\\?\DISPLAY#ABC#1",
                    new MonitorBounds(10, 20, 1920, 1080),
                    @"C:\Wallpapers\one.jpg"),
            ]));

        var monitors = await detector.DetectAsync();

        var monitor = Assert.Single(monitors);
        Assert.Equal(@"\\?\DISPLAY#ABC#1", monitor.Identity.MonitorKey);
        Assert.Equal("ABC", monitor.Identity.DeviceName);
        Assert.Equal("Monitor 1 - ABC", monitor.DisplayName);
        Assert.Equal(1920, monitor.Identity.Width);
        Assert.Equal(1080, monitor.Identity.Height);
        Assert.Equal(10, monitor.Identity.X);
        Assert.Equal(20, monitor.Identity.Y);
        Assert.Equal(WallpaperSourceKind.Image, monitor.CurrentSource.Kind);
        Assert.Equal(@"C:\Wallpapers\one.jpg", monitor.CurrentSource.ImagePath);
        Assert.NotNull(monitor.CurrentPlacement);
        Assert.Equal(WallpaperFitMode.Contain, monitor.CurrentPlacement.FitMode);
    }

    [Fact]
    public async Task WindowsMonitorDetector_UsesBackgroundSourceWhenMonitorWallpaperIsEmpty()
    {
        var detector = new WindowsMonitorDetector(new FixedDesktopWallpaperReader(
            WallpaperPlacement.Default,
            WallpaperSource.FromSolidColor("#112233"),
            [
                new DesktopWallpaperSnapshot(
                    "DISPLAY-1",
                    new MonitorBounds(0, 0, 1920, 1080),
                    null),
            ]));

        var monitors = await detector.DetectAsync();

        var monitor = Assert.Single(monitors);
        Assert.Equal(WallpaperSourceKind.SolidColor, monitor.CurrentSource.Kind);
        Assert.Equal("#112233", monitor.CurrentSource.ColorHex);
    }

    [Fact]
    public async Task BasicPngWallpaperRenderer_RendersSolidColorAtMonitorSize()
    {
        var root = Path.Combine(Path.GetTempPath(), $"waller-render-tests-{Guid.NewGuid():N}");
        try
        {
            var renderer = new BasicPngWallpaperRenderer(new RenderedWallpaperStore(root));
            var monitor = CreateMonitor("DISPLAY-1", 32, 18, WallpaperSource.Empty);
            var assignment = new PresetAssignment(
                monitor.Identity,
                WallpaperSource.FromSolidColor("#336699"),
                WallpaperPlacement.Default);

            var rendered = await renderer.RenderAsync(new RenderRequest(monitor, assignment));
            var (width, height) = ReadPngSize(rendered.Path);

            Assert.True(File.Exists(rendered.Path));
            Assert.Equal(32, width);
            Assert.Equal(18, height);
            Assert.Equal(32, rendered.Width);
            Assert.Equal(18, rendered.Height);
        }
        finally
        {
            if (Directory.Exists(root))
            {
                Directory.Delete(root, recursive: true);
            }
        }
    }

    [Fact]
    public async Task BasicPngWallpaperRenderer_ThrowsStableErrorForMissingImageSource()
    {
        var root = Path.Combine(Path.GetTempPath(), $"waller-render-tests-{Guid.NewGuid():N}");
        try
        {
            var renderer = new BasicPngWallpaperRenderer(new RenderedWallpaperStore(root));
            var monitor = CreateMonitor("DISPLAY-1", 32, 18, WallpaperSource.Empty);
            var assignment = new PresetAssignment(
                monitor.Identity,
                WallpaperSource.FromImage(@"C:\missing-image.png"),
                WallpaperPlacement.Default);

            var error = await Assert.ThrowsAsync<WallpaperRenderException>(
                () => renderer.RenderAsync(new RenderRequest(monitor, assignment)));

            Assert.Equal(ApplyErrorCodes.MissingImageSource, error.ErrorCode);
        }
        finally
        {
            if (Directory.Exists(root))
            {
                Directory.Delete(root, recursive: true);
            }
        }
    }

    [Fact]
    public async Task BasicPngWallpaperRenderer_StretchesImageSource()
    {
        var root = Path.Combine(Path.GetTempPath(), $"waller-render-tests-{Guid.NewGuid():N}");
        try
        {
            var sourcePath = Path.Combine(root, "source.png");
            Directory.CreateDirectory(root);
            await WriteTwoColorSourceAsync(sourcePath);

            var renderer = new BasicPngWallpaperRenderer(new RenderedWallpaperStore(root));
            var monitor = CreateMonitor("DISPLAY-1", 4, 1, WallpaperSource.Empty);
            var assignment = new PresetAssignment(
                monitor.Identity,
                WallpaperSource.FromImage(sourcePath),
                new WallpaperPlacement(WallpaperFitMode.Stretch, WallpaperAnchor.Center));

            var rendered = await renderer.RenderAsync(new RenderRequest(monitor, assignment));
            var pixels = ReadPngPixels(rendered.Path);

            Assert.Equal(new RgbColor(255, 0, 0), pixels.GetPixel(0, 0));
            Assert.Equal(new RgbColor(255, 0, 0), pixels.GetPixel(1, 0));
            Assert.Equal(new RgbColor(0, 255, 0), pixels.GetPixel(2, 0));
            Assert.Equal(new RgbColor(0, 255, 0), pixels.GetPixel(3, 0));
        }
        finally
        {
            if (Directory.Exists(root))
            {
                Directory.Delete(root, recursive: true);
            }
        }
    }

    [Fact]
    public async Task BasicPngWallpaperRenderer_TilesImageSource()
    {
        var root = Path.Combine(Path.GetTempPath(), $"waller-render-tests-{Guid.NewGuid():N}");
        try
        {
            var sourcePath = Path.Combine(root, "source.png");
            Directory.CreateDirectory(root);
            await WriteTwoColorSourceAsync(sourcePath);

            var renderer = new BasicPngWallpaperRenderer(new RenderedWallpaperStore(root));
            var monitor = CreateMonitor("DISPLAY-1", 5, 1, WallpaperSource.Empty);
            var assignment = new PresetAssignment(
                monitor.Identity,
                WallpaperSource.FromImage(sourcePath),
                new WallpaperPlacement(WallpaperFitMode.Tile, WallpaperAnchor.Center));

            var rendered = await renderer.RenderAsync(new RenderRequest(monitor, assignment));
            var pixels = ReadPngPixels(rendered.Path);

            Assert.Equal(new RgbColor(255, 0, 0), pixels.GetPixel(0, 0));
            Assert.Equal(new RgbColor(0, 255, 0), pixels.GetPixel(1, 0));
            Assert.Equal(new RgbColor(255, 0, 0), pixels.GetPixel(2, 0));
            Assert.Equal(new RgbColor(0, 255, 0), pixels.GetPixel(3, 0));
            Assert.Equal(new RgbColor(255, 0, 0), pixels.GetPixel(4, 0));
        }
        finally
        {
            if (Directory.Exists(root))
            {
                Directory.Delete(root, recursive: true);
            }
        }
    }

    [Fact]
    public async Task BasicPngWallpaperRenderer_CoverUsesAnchorForCrop()
    {
        var root = Path.Combine(Path.GetTempPath(), $"waller-render-tests-{Guid.NewGuid():N}");
        try
        {
            var sourcePath = Path.Combine(root, "source.png");
            Directory.CreateDirectory(root);
            await WriteTwoColorSourceAsync(sourcePath);

            var renderer = new BasicPngWallpaperRenderer(new RenderedWallpaperStore(root));
            var monitor = CreateMonitor("DISPLAY-1", 1, 1, WallpaperSource.Empty);
            var assignment = new PresetAssignment(
                monitor.Identity,
                WallpaperSource.FromImage(sourcePath),
                new WallpaperPlacement(WallpaperFitMode.Cover, WallpaperAnchor.Right));

            var rendered = await renderer.RenderAsync(new RenderRequest(monitor, assignment));
            var pixels = ReadPngPixels(rendered.Path);

            Assert.Equal(new RgbColor(0, 255, 0), pixels.GetPixel(0, 0));
        }
        finally
        {
            if (Directory.Exists(root))
            {
                Directory.Delete(root, recursive: true);
            }
        }
    }

    [Fact]
    public async Task BasicPngWallpaperRenderer_CoverUsesOffsetForCrop()
    {
        var root = Path.Combine(Path.GetTempPath(), $"waller-render-tests-{Guid.NewGuid():N}");
        try
        {
            var sourcePath = Path.Combine(root, "source.png");
            Directory.CreateDirectory(root);
            await WriteFourColorSourceAsync(sourcePath);

            var renderer = new BasicPngWallpaperRenderer(new RenderedWallpaperStore(root));
            var monitor = CreateMonitor("DISPLAY-1", 2, 1, WallpaperSource.Empty);
            var assignment = new PresetAssignment(
                monitor.Identity,
                WallpaperSource.FromImage(sourcePath),
                new WallpaperPlacement(WallpaperFitMode.Cover, WallpaperAnchor.Center, -100, 0));

            var rendered = await renderer.RenderAsync(new RenderRequest(monitor, assignment));
            var pixels = ReadPngPixels(rendered.Path);

            Assert.Equal(new RgbColor(0, 0, 255), pixels.GetPixel(0, 0));
            Assert.Equal(new RgbColor(255, 255, 255), pixels.GetPixel(1, 0));
        }
        finally
        {
            if (Directory.Exists(root))
            {
                Directory.Delete(root, recursive: true);
            }
        }
    }

    [Fact]
    public async Task BasicPngWallpaperRenderer_ContainKeepsBlackBands()
    {
        var root = Path.Combine(Path.GetTempPath(), $"waller-render-tests-{Guid.NewGuid():N}");
        try
        {
            var sourcePath = Path.Combine(root, "source.png");
            Directory.CreateDirectory(root);
            await WriteTwoColorSourceAsync(sourcePath);

            var renderer = new BasicPngWallpaperRenderer(new RenderedWallpaperStore(root));
            var monitor = CreateMonitor("DISPLAY-1", 4, 4, WallpaperSource.Empty);
            var assignment = new PresetAssignment(
                monitor.Identity,
                WallpaperSource.FromImage(sourcePath),
                new WallpaperPlacement(WallpaperFitMode.Contain, WallpaperAnchor.Center));

            var rendered = await renderer.RenderAsync(new RenderRequest(monitor, assignment));
            var pixels = ReadPngPixels(rendered.Path);

            Assert.Equal(RgbColor.Black, pixels.GetPixel(0, 0));
            Assert.Equal(new RgbColor(255, 0, 0), pixels.GetPixel(0, 1));
            Assert.Equal(new RgbColor(0, 255, 0), pixels.GetPixel(3, 2));
            Assert.Equal(RgbColor.Black, pixels.GetPixel(3, 3));
        }
        finally
        {
            if (Directory.Exists(root))
            {
                Directory.Delete(root, recursive: true);
            }
        }
    }

    [Fact]
    public void ImagePlacementPlan_CalculatesCoverCropWithOffset()
    {
        var plan = ImagePlacementPlan.Create(
            sourceWidth: 4,
            sourceHeight: 1,
            targetWidth: 2,
            targetHeight: 1,
            new WallpaperPlacement(WallpaperFitMode.Cover, WallpaperAnchor.Center, -100, 0));

        Assert.False(plan.IsTile);
        Assert.Equal(-2, plan.OriginX);
        Assert.Equal(0, plan.OriginY);
        Assert.Equal(4, plan.DrawWidth);
        Assert.Equal(1, plan.DrawHeight);
    }

    [Fact]
    public void ImagePlacementPlan_CalculatesContainBlackBandArea()
    {
        var plan = ImagePlacementPlan.Create(
            sourceWidth: 2,
            sourceHeight: 1,
            targetWidth: 4,
            targetHeight: 4,
            new WallpaperPlacement(WallpaperFitMode.Contain, WallpaperAnchor.Center));

        Assert.False(plan.IsTile);
        Assert.Equal(0, plan.OriginX);
        Assert.Equal(1, plan.OriginY);
        Assert.Equal(4, plan.DrawWidth);
        Assert.Equal(2, plan.DrawHeight);
    }

    [Fact]
    public void ImagePlacementPlan_MarksTileWithoutScaling()
    {
        var plan = ImagePlacementPlan.Create(
            sourceWidth: 2,
            sourceHeight: 1,
            targetWidth: 5,
            targetHeight: 3,
            new WallpaperPlacement(WallpaperFitMode.Tile, WallpaperAnchor.BottomRight, 100, -100));

        Assert.True(plan.IsTile);
        Assert.Equal(0, plan.OriginX);
        Assert.Equal(0, plan.OriginY);
        Assert.Equal(2, plan.DrawWidth);
        Assert.Equal(1, plan.DrawHeight);
    }

    [Fact]
    public void ImagePlacementPlan_CalculatesStretchToTarget()
    {
        var plan = ImagePlacementPlan.Create(
            sourceWidth: 2,
            sourceHeight: 1,
            targetWidth: 5,
            targetHeight: 3,
            new WallpaperPlacement(WallpaperFitMode.Stretch, WallpaperAnchor.BottomRight));

        Assert.False(plan.IsTile);
        Assert.Equal(0, plan.OriginX);
        Assert.Equal(0, plan.OriginY);
        Assert.Equal(5, plan.DrawWidth);
        Assert.Equal(3, plan.DrawHeight);
    }

    [Fact]
    public void ImagePlacementPlan_CalculatesCenterWithoutScaling()
    {
        var plan = ImagePlacementPlan.Create(
            sourceWidth: 2,
            sourceHeight: 1,
            targetWidth: 6,
            targetHeight: 5,
            new WallpaperPlacement(WallpaperFitMode.Center, WallpaperAnchor.Center));

        Assert.False(plan.IsTile);
        Assert.Equal(2, plan.OriginX);
        Assert.Equal(2, plan.OriginY);
        Assert.Equal(2, plan.DrawWidth);
        Assert.Equal(1, plan.DrawHeight);
    }

    [Fact]
    public void ImagePlacementPlan_RejectsNonPositiveDimensions()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            ImagePlacementPlan.Create(
                sourceWidth: 0,
                sourceHeight: 1,
                targetWidth: 2,
                targetHeight: 2,
                WallpaperPlacement.Default));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            ImagePlacementPlan.Create(
                sourceWidth: 1,
                sourceHeight: 0,
                targetWidth: 2,
                targetHeight: 2,
                WallpaperPlacement.Default));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            ImagePlacementPlan.Create(
                sourceWidth: 1,
                sourceHeight: 1,
                targetWidth: 0,
                targetHeight: 2,
                WallpaperPlacement.Default));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            ImagePlacementPlan.Create(
                sourceWidth: 1,
                sourceHeight: 1,
                targetWidth: 2,
                targetHeight: 0,
                WallpaperPlacement.Default));
    }

    [Fact]
    public async Task WallpaperApplyService_AppliesRenderedSolidColor()
    {
        var root = Path.Combine(Path.GetTempPath(), $"waller-apply-tests-{Guid.NewGuid():N}");
        try
        {
            var applier = new RecordingWallpaperApplier(succeed: true);
            var service = CreateApplyService(root, applier);
            var monitor = CreateMonitor("DISPLAY-1", 16, 16, WallpaperSource.FromSolidColor("#112233"));
            var session = ActiveSession.FromMonitors([monitor]);

            var result = await service.ApplyMonitorAsync(session, "DISPLAY-1");

            Assert.Equal(1, result.Succeeded);
            Assert.Equal(0, result.Failed);
            Assert.Equal(MonitorApplyStatus.Applied, result.Session.Monitors[0].ApplyStatus);
            Assert.NotNull(applier.LastWallpaper);
            Assert.True(File.Exists(applier.LastWallpaper.Path));
        }
        finally
        {
            if (Directory.Exists(root))
            {
                Directory.Delete(root, recursive: true);
            }
        }
    }

    [Fact]
    public async Task WallpaperApplyService_AppliesMonitorKeyCaseInsensitively()
    {
        var root = Path.Combine(Path.GetTempPath(), $"waller-apply-tests-{Guid.NewGuid():N}");
        try
        {
            var applier = new RecordingWallpaperApplier(succeed: true);
            var service = CreateApplyService(root, applier);
            var monitor = CreateMonitor("DISPLAY-1", 16, 16, WallpaperSource.FromSolidColor("#112233"));
            var session = ActiveSession.FromMonitors([monitor]);

            var result = await service.ApplyMonitorAsync(session, "display-1");

            Assert.Equal(1, result.Succeeded);
            Assert.Equal(0, result.Failed);
            Assert.Equal(MonitorApplyStatus.Applied, result.Session.Monitors[0].ApplyStatus);
            Assert.NotNull(applier.LastWallpaper);
        }
        finally
        {
            if (Directory.Exists(root))
            {
                Directory.Delete(root, recursive: true);
            }
        }
    }

    [Fact]
    public async Task WallpaperApplyService_DoesNotRenderWhenMonitorKeyIsUnknown()
    {
        var root = Path.Combine(Path.GetTempPath(), $"waller-apply-tests-{Guid.NewGuid():N}");
        try
        {
            var applier = new RecordingWallpaperApplier(succeed: true);
            var service = CreateApplyService(root, applier);
            var monitor = CreateMonitor("DISPLAY-1", 16, 16, WallpaperSource.FromSolidColor("#112233"));
            var session = ActiveSession.FromMonitors([monitor]);

            var result = await service.ApplyMonitorAsync(session, "MISSING");

            Assert.Equal(0, result.Succeeded);
            Assert.Equal(0, result.Failed);
            Assert.Equal(MonitorApplyStatus.Clean, result.Session.Monitors[0].ApplyStatus);
            Assert.Null(applier.LastWallpaper);
            Assert.False(Directory.Exists(Path.Combine(root, "rendered")));
        }
        finally
        {
            if (Directory.Exists(root))
            {
                Directory.Delete(root, recursive: true);
            }
        }
    }

    [Fact]
    public async Task WallpaperApplyService_FailsImageSourceBeforeWindowsApply()
    {
        var root = Path.Combine(Path.GetTempPath(), $"waller-apply-tests-{Guid.NewGuid():N}");
        try
        {
            var applier = new RecordingWallpaperApplier(succeed: true);
            var service = CreateApplyService(root, applier);
            var monitor = CreateMonitor("DISPLAY-1", 16, 16, WallpaperSource.FromImage(@"C:\missing-image.png"));
            var session = ActiveSession.FromMonitors([monitor]);

            var result = await service.ApplyMonitorAsync(session, "DISPLAY-1");

            Assert.Equal(0, result.Succeeded);
            Assert.Equal(1, result.Failed);
            Assert.Equal(MonitorApplyStatus.Error, result.Session.Monitors[0].ApplyStatus);
            Assert.Equal(ApplyErrorCodes.MissingImageSource, result.Session.Monitors[0].ApplyError);
            Assert.Null(applier.LastWallpaper);
        }
        finally
        {
            if (Directory.Exists(root))
            {
                Directory.Delete(root, recursive: true);
            }
        }
    }

    [Fact]
    public async Task WallpaperApplyService_ApplyMonitorReadySourceSkipsMissingImage()
    {
        var root = Path.Combine(Path.GetTempPath(), $"waller-apply-tests-{Guid.NewGuid():N}");
        try
        {
            var renderer = new BasicPngWallpaperRenderer(new RenderedWallpaperStore(root));
            var applier = new RecordingWallpaperApplier(succeed: true);
            var service = new WallpaperApplyService(renderer, applier);
            var first = CreateMonitor("DISPLAY-1", 16, 16, WallpaperSource.FromSolidColor("#112233"));
            var second = CreateMonitor("DISPLAY-2", 16, 16, WallpaperSource.FromImage(@"C:\missing-image.png"));
            var session = ActiveSession.FromMonitors([first, second]);

            var result = await service.ApplyMonitorReadySourceAsync(session, "display-2");

            Assert.Equal(0, result.Succeeded);
            Assert.Equal(0, result.Failed);
            Assert.Equal(1, result.Skipped);
            Assert.Equal(MonitorApplyStatus.Clean, result.Session.Monitors[0].ApplyStatus);
            Assert.Equal(MonitorApplyStatus.Error, result.Session.Monitors[1].ApplyStatus);
            Assert.Equal(ApplyErrorCodes.MissingImageSource, result.Session.Monitors[1].ApplyError);
            Assert.Null(applier.LastWallpaper);
        }
        finally
        {
            if (Directory.Exists(root))
            {
                Directory.Delete(root, recursive: true);
            }
        }
    }

    [Fact]
    public async Task WallpaperApplyService_ApplyMonitorReadySourceUsesPreflightReadyTarget()
    {
        var root = Path.Combine(Path.GetTempPath(), $"waller-apply-tests-{Guid.NewGuid():N}");
        try
        {
            var renderer = new BasicPngWallpaperRenderer(new RenderedWallpaperStore(root));
            var applier = new RecordingWallpaperApplier(succeed: true);
            var service = new WallpaperApplyService(renderer, applier);
            var first = CreateMonitor("DISPLAY-1", 16, 16, WallpaperSource.FromSolidColor("#112233"));
            var second = CreateMonitor("DISPLAY-2", 16, 16, WallpaperSource.FromImage(@"C:\missing-image.png"));
            var session = ActiveSession.FromMonitors([first, second]);

            var result = await service.ApplyMonitorReadySourceAsync(session, "display-1");

            Assert.Equal(1, result.Succeeded);
            Assert.Equal(0, result.Failed);
            Assert.Equal(0, result.Skipped);
            Assert.Equal(MonitorApplyStatus.Applied, result.Session.Monitors[0].ApplyStatus);
            Assert.Equal(MonitorApplyStatus.Clean, result.Session.Monitors[1].ApplyStatus);
            Assert.NotNull(applier.LastWallpaper);
            Assert.Equal("DISPLAY-1", applier.LastWallpaper.Monitor.MonitorKey);
        }
        finally
        {
            if (Directory.Exists(root))
            {
                Directory.Delete(root, recursive: true);
            }
        }
    }

    [Fact]
    public async Task WallpaperApplyService_UsesFriendlyErrorWhenWindowsApplyFails()
    {
        var root = Path.Combine(Path.GetTempPath(), $"waller-apply-tests-{Guid.NewGuid():N}");
        try
        {
            var renderer = new BasicPngWallpaperRenderer(new RenderedWallpaperStore(root));
            var applier = new RecordingWallpaperApplier(succeed: false);
            var service = new WallpaperApplyService(renderer, applier);
            var monitor = CreateMonitor("DISPLAY-1", 16, 16, WallpaperSource.FromSolidColor("#112233"));
            var session = ActiveSession.FromMonitors([monitor]);

            var result = await service.ApplyMonitorAsync(session, "DISPLAY-1");

            Assert.Equal(0, result.Succeeded);
            Assert.Equal(1, result.Failed);
            Assert.Equal(MonitorApplyStatus.Error, result.Session.Monitors[0].ApplyStatus);
            Assert.Equal(ApplyErrorCodes.WallpaperApplyFailed, result.Session.Monitors[0].ApplyError);
        }
        finally
        {
            if (Directory.Exists(root))
            {
                Directory.Delete(root, recursive: true);
            }
        }
    }

    [Fact]
    public async Task WallpaperApplyService_UsesFriendlyErrorWhenRenderUnexpectedlyFails()
    {
        var renderer = new ThrowingWallpaperRenderer(new InvalidOperationException("boom"));
        var applier = new RecordingWallpaperApplier(succeed: true);
        var service = new WallpaperApplyService(renderer, applier);
        var monitor = CreateMonitor("DISPLAY-1", 16, 16, WallpaperSource.FromSolidColor("#112233"));
        var session = ActiveSession.FromMonitors([monitor]);

        var result = await service.ApplyMonitorAsync(session, "DISPLAY-1");

        Assert.Equal(0, result.Succeeded);
        Assert.Equal(1, result.Failed);
        Assert.Equal(MonitorApplyStatus.Error, result.Session.Monitors[0].ApplyStatus);
        Assert.Equal(ApplyErrorCodes.WallpaperApplyFailed, result.Session.Monitors[0].ApplyError);
        Assert.Null(applier.LastWallpaper);
    }

    [Fact]
    public async Task WallpaperApplyService_UsesApplierErrorCodeWithoutParsingMessage()
    {
        var root = Path.Combine(Path.GetTempPath(), $"waller-apply-tests-{Guid.NewGuid():N}");
        try
        {
            var renderer = new BasicPngWallpaperRenderer(new RenderedWallpaperStore(root));
            var applier = new RecordingWallpaperApplier(
                succeed: false,
                errorCode: ApplyErrorCodes.RenderedWallpaperMissing,
                errorMessage: "arbitrary technical failure");
            var service = new WallpaperApplyService(renderer, applier);
            var monitor = CreateMonitor("DISPLAY-1", 16, 16, WallpaperSource.FromSolidColor("#112233"));
            var session = ActiveSession.FromMonitors([monitor]);

            var result = await service.ApplyMonitorAsync(session, "DISPLAY-1");

            Assert.Equal(0, result.Succeeded);
            Assert.Equal(1, result.Failed);
            Assert.Equal(ApplyErrorCodes.RenderedWallpaperMissing, result.Session.Monitors[0].ApplyError);
        }
        finally
        {
            if (Directory.Exists(root))
            {
                Directory.Delete(root, recursive: true);
            }
        }
    }

    [Fact]
    public async Task WallpaperApplyService_PropagatesCancellationWithoutMarkingApplyFailure()
    {
        var renderer = new CancelingWallpaperRenderer();
        var applier = new RecordingWallpaperApplier(succeed: true);
        var service = new WallpaperApplyService(renderer, applier);
        var monitor = CreateMonitor("DISPLAY-1", 16, 16, WallpaperSource.FromSolidColor("#112233"));
        var session = ActiveSession.FromMonitors([monitor]);
        using var cancellation = new CancellationTokenSource();
        renderer.Cancellation = cancellation;

        var error = await Assert.ThrowsAsync<ApplyCanceledException>(() =>
            service.ApplyMonitorAsync(session, "DISPLAY-1", cancellationToken: cancellation.Token));

        Assert.Equal(0, error.Result.Succeeded);
        Assert.Equal(0, error.Result.Failed);
        Assert.Equal(MonitorApplyStatus.Clean, error.Result.Session.Monitors[0].ApplyStatus);
        Assert.Null(applier.LastWallpaper);
    }

    [Fact]
    public async Task WallpaperApplyService_CancelledApplyPreservesPartialSuccess()
    {
        var renderer = new PassthroughWallpaperRenderer();
        var applier = new CancelOnSecondWallpaperApplier();
        var service = new WallpaperApplyService(renderer, applier);
        var first = CreateMonitor("DISPLAY-1", 16, 16, WallpaperSource.FromSolidColor("#112233"));
        var second = CreateMonitor("DISPLAY-2", 16, 16, WallpaperSource.FromSolidColor("#445566"));
        var session = ActiveSession.FromMonitors([first, second]);
        using var cancellation = new CancellationTokenSource();
        applier.Cancellation = cancellation;

        var error = await Assert.ThrowsAsync<ApplyCanceledException>(() =>
            service.ApplyAllAsync(session, cancellationToken: cancellation.Token));

        Assert.Equal(1, error.Result.Succeeded);
        Assert.Equal(0, error.Result.Failed);
        Assert.Equal(MonitorApplyStatus.Applied, error.Result.Session.Monitors[0].ApplyStatus);
        Assert.Equal(MonitorApplyStatus.Clean, error.Result.Session.Monitors[1].ApplyStatus);
    }

    [Fact]
    public void ApplyErrorClassifier_MapsUnknownErrorsToFriendlyFallback()
    {
        Assert.Equal(
            ApplyErrorCodes.WallpaperApplyFailed,
            ApplyErrorClassifier.FriendlyErrorCode("unexpected code"));
        Assert.Equal(
            ApplyErrorCodes.WallpaperApplyFailed,
            ApplyErrorClassifier.FriendlyErrorCode(new InvalidOperationException("boom")));
    }

    [Fact]
    public void ApplyErrorClassifier_PreservesKnownRenderErrorCodes()
    {
        var error = new WallpaperRenderException(
            ApplyErrorCodes.MissingImageSource,
            "missing");

        Assert.Equal(
            ApplyErrorCodes.MissingImageSource,
            ApplyErrorClassifier.FriendlyErrorCode(error));
    }

    [Fact]
    public void ApplySessionResult_ReportsWhetherAnyOutcomeExists()
    {
        var session = ActiveSession.FromMonitors([]);

        Assert.False(new ApplySessionResult(session, Succeeded: 0, Failed: 0, Skipped: 0).HasAnyOutcome);
        Assert.True(new ApplySessionResult(session, Succeeded: 1, Failed: 0, Skipped: 0).HasAnyOutcome);
        Assert.True(new ApplySessionResult(session, Succeeded: 0, Failed: 1, Skipped: 0).HasAnyOutcome);
        Assert.True(new ApplySessionResult(session, Succeeded: 0, Failed: 0, Skipped: 1).HasAnyOutcome);
    }

    [Fact]
    public void ApplySessionResult_ReportsWhetherAnyMonitorWasAppliedOrFailed()
    {
        var session = ActiveSession.FromMonitors([]);

        Assert.False(new ApplySessionResult(session, Succeeded: 0, Failed: 0, Skipped: 0).HasAppliedOutcome);
        Assert.True(new ApplySessionResult(session, Succeeded: 1, Failed: 0, Skipped: 0).HasAppliedOutcome);
        Assert.True(new ApplySessionResult(session, Succeeded: 0, Failed: 1, Skipped: 0).HasAppliedOutcome);
        Assert.False(new ApplySessionResult(session, Succeeded: 0, Failed: 0, Skipped: 1).HasAppliedOutcome);
    }

    [Fact]
    public void ApplySessionResult_WithSkippedPreservesApplyCounts()
    {
        var session = ActiveSession.FromMonitors([]);
        var result = new ApplySessionResult(session, Succeeded: 2, Failed: 1);

        var withSkipped = result.WithSkipped(3);

        Assert.Equal(2, withSkipped.Succeeded);
        Assert.Equal(1, withSkipped.Failed);
        Assert.Equal(3, withSkipped.Skipped);
        Assert.Same(session, withSkipped.Session);
    }

    [Fact]
    public void ApplyCanceledException_WithSkippedPreservesPartialResult()
    {
        var session = ActiveSession.FromMonitors([]);
        var original = new ApplyCanceledException(new ApplySessionResult(session, Succeeded: 1, Failed: 0));

        var withSkipped = original.WithSkipped(2);

        Assert.Equal(1, withSkipped.Result.Succeeded);
        Assert.Equal(0, withSkipped.Result.Failed);
        Assert.Equal(2, withSkipped.Result.Skipped);
        Assert.Same(session, withSkipped.Result.Session);
        Assert.Same(original, withSkipped.InnerException);
    }

    [Fact]
    public void ApplyRunTracker_CreatesSessionResultFromCurrentMonitorState()
    {
        var first = CreateMonitor("DISPLAY-1", 16, 16, WallpaperSource.FromSolidColor("#112233"));
        var second = CreateMonitor("DISPLAY-2", 16, 16, WallpaperSource.FromSolidColor("#445566"));
        var session = ActiveSession.FromMonitors([first, second]);
        var updatedMonitors = session.Monitors
            .Select((monitor, index) => index == 0 ? monitor.WithAppliedAssignment() : monitor.WithApplyError("failed"))
            .ToList();
        var tracker = new ApplyRunTracker(total: 2, progress: null);

        tracker.RecordSuccess();
        tracker.RecordFailure();

        var result = tracker.ToResult(session, updatedMonitors);

        Assert.Equal(1, result.Succeeded);
        Assert.Equal(1, result.Failed);
        Assert.Equal(MonitorApplyStatus.Applied, result.Session.Monitors[0].ApplyStatus);
        Assert.Equal(MonitorApplyStatus.Error, result.Session.Monitors[1].ApplyStatus);
        Assert.Equal(MonitorApplyStatus.Clean, session.Monitors[0].ApplyStatus);
    }

    [Fact]
    public void ApplyRunTracker_CreatesCanceledExceptionFromCurrentMonitorState()
    {
        var first = CreateMonitor("DISPLAY-1", 16, 16, WallpaperSource.FromSolidColor("#112233"));
        var second = CreateMonitor("DISPLAY-2", 16, 16, WallpaperSource.FromSolidColor("#445566"));
        var session = ActiveSession.FromMonitors([first, second]);
        var updatedMonitors = session.Monitors
            .Select((monitor, index) => index == 0 ? monitor.WithAppliedAssignment() : monitor)
            .ToList();
        var tracker = new ApplyRunTracker(total: 2, progress: null);

        tracker.RecordSuccess();

        var error = tracker.ToCanceledException(session, updatedMonitors);

        Assert.Equal(1, error.Result.Succeeded);
        Assert.Equal(0, error.Result.Failed);
        Assert.Equal(MonitorApplyStatus.Applied, error.Result.Session.Monitors[0].ApplyStatus);
        Assert.Equal(MonitorApplyStatus.Clean, error.Result.Session.Monitors[1].ApplyStatus);
        Assert.Equal(MonitorApplyStatus.Clean, session.Monitors[0].ApplyStatus);
    }

    [Fact]
    public void ApplyRunTracker_RecordsStepResultOutcome()
    {
        var successMonitor = CreateMonitor("DISPLAY-1", 16, 16, WallpaperSource.FromSolidColor("#112233"));
        var failedMonitor = CreateMonitor("DISPLAY-2", 16, 16, WallpaperSource.FromSolidColor("#445566"));
        var session = ActiveSession.FromMonitors([successMonitor, failedMonitor]);
        var tracker = new ApplyRunTracker(total: 2, progress: null);

        tracker.Record(MonitorApplyStepResult.Success(session.Monitors[0]));
        tracker.Record(MonitorApplyStepResult.Failure(session.Monitors[1], "failed"));

        Assert.Equal(1, tracker.Succeeded);
        Assert.Equal(1, tracker.Failed);
    }

    [Fact]
    public async Task WallpaperApplyService_AppliesOnlyMatchingMonitors()
    {
        var root = Path.Combine(Path.GetTempPath(), $"waller-apply-tests-{Guid.NewGuid():N}");
        try
        {
            var renderer = new BasicPngWallpaperRenderer(new RenderedWallpaperStore(root));
            var applier = new RecordingWallpaperApplier(succeed: true);
            var service = new WallpaperApplyService(renderer, applier);
            var first = CreateMonitor("DISPLAY-1", 16, 16, WallpaperSource.FromSolidColor("#112233"));
            var second = CreateMonitor("DISPLAY-2", 16, 16, WallpaperSource.FromImage(@"C:\missing-image.png"));
            var session = ActiveSession.FromMonitors([first, second]);

            var result = await service.ApplyMatchingAsync(
                session,
                monitor => monitor.Monitor.Identity.MonitorKey == "DISPLAY-1");

            Assert.Equal(1, result.Succeeded);
            Assert.Equal(0, result.Failed);
            Assert.Equal(MonitorApplyStatus.Applied, result.Session.Monitors[0].ApplyStatus);
            Assert.Equal(MonitorApplyStatus.Clean, result.Session.Monitors[1].ApplyStatus);
        }
        finally
        {
            if (Directory.Exists(root))
            {
                Directory.Delete(root, recursive: true);
            }
        }
    }

    [Fact]
    public async Task WallpaperApplyService_ApplyAllReadySourcesSkipsMissingImages()
    {
        var root = Path.Combine(Path.GetTempPath(), $"waller-apply-tests-{Guid.NewGuid():N}");
        try
        {
            var renderer = new BasicPngWallpaperRenderer(new RenderedWallpaperStore(root));
            var applier = new RecordingWallpaperApplier(succeed: true);
            var service = new WallpaperApplyService(renderer, applier);
            var first = CreateMonitor("DISPLAY-1", 16, 16, WallpaperSource.FromSolidColor("#112233"));
            var second = CreateMonitor("DISPLAY-2", 16, 16, WallpaperSource.FromImage(@"C:\missing-image.png"));
            var session = ActiveSession.FromMonitors([first, second]);

            var result = await service.ApplyAllReadySourcesAsync(session);

            Assert.Equal(1, result.Succeeded);
            Assert.Equal(0, result.Failed);
            Assert.Equal(1, result.Skipped);
            Assert.Equal(MonitorApplyStatus.Applied, result.Session.Monitors[0].ApplyStatus);
            Assert.Equal(MonitorApplyStatus.Error, result.Session.Monitors[1].ApplyStatus);
            Assert.Equal(ApplyErrorCodes.MissingImageSource, result.Session.Monitors[1].ApplyError);
            Assert.NotNull(applier.LastWallpaper);
            Assert.Equal("DISPLAY-1", applier.LastWallpaper.Monitor.MonitorKey);
        }
        finally
        {
            if (Directory.Exists(root))
            {
                Directory.Delete(root, recursive: true);
            }
        }
    }

    [Fact]
    public async Task WallpaperApplyService_ApplyAllReadySourcesSkipsAllMissingImagesWithoutRendering()
    {
        var root = Path.Combine(Path.GetTempPath(), $"waller-apply-tests-{Guid.NewGuid():N}");
        try
        {
            var renderer = new BasicPngWallpaperRenderer(new RenderedWallpaperStore(root));
            var applier = new RecordingWallpaperApplier(succeed: true);
            var service = new WallpaperApplyService(renderer, applier);
            var first = CreateMonitor("DISPLAY-1", 16, 16, WallpaperSource.FromImage(@"C:\missing-image-a.png"));
            var second = CreateMonitor("DISPLAY-2", 16, 16, WallpaperSource.FromImage(@"C:\missing-image-b.png"));
            var session = ActiveSession.FromMonitors([first, second]);

            var result = await service.ApplyAllReadySourcesAsync(session);

            Assert.Equal(0, result.Succeeded);
            Assert.Equal(0, result.Failed);
            Assert.Equal(2, result.Skipped);
            Assert.All(result.Session.Monitors, monitor =>
            {
                Assert.Equal(MonitorApplyStatus.Error, monitor.ApplyStatus);
                Assert.Equal(ApplyErrorCodes.MissingImageSource, monitor.ApplyError);
            });
            Assert.Null(applier.LastWallpaper);
            Assert.False(Directory.Exists(Path.Combine(root, "rendered")));
        }
        finally
        {
            if (Directory.Exists(root))
            {
                Directory.Delete(root, recursive: true);
            }
        }
    }

    [Fact]
    public async Task WallpaperApplyService_ReportsProgress()
    {
        var root = Path.Combine(Path.GetTempPath(), $"waller-apply-tests-{Guid.NewGuid():N}");
        try
        {
            var renderer = new BasicPngWallpaperRenderer(new RenderedWallpaperStore(root));
            var applier = new RecordingWallpaperApplier(succeed: true);
            var service = new WallpaperApplyService(renderer, applier);
            var first = CreateMonitor("DISPLAY-1", 16, 16, WallpaperSource.FromSolidColor("#112233"));
            var second = CreateMonitor("DISPLAY-2", 16, 16, WallpaperSource.FromSolidColor("#445566"));
            var session = ActiveSession.FromMonitors([first, second]);
            var progressEvents = new List<ApplyProgress>();

            await service.ApplyAllAsync(session, progress => progressEvents.Add(progress));

            Assert.Contains(progressEvents, progress =>
                progress.Completed == 0
                && progress.Total == 2
                && progress.Status == MonitorApplyStatus.Applying);
            Assert.Contains(progressEvents, progress =>
                progress.Completed == 2
                && progress.Total == 2
                && progress.Status == MonitorApplyStatus.Applied);
        }
        finally
        {
            if (Directory.Exists(root))
            {
                Directory.Delete(root, recursive: true);
            }
        }
    }

    private static Preset CreatePreset(IReadOnlyList<PresetAssignment> assignments)
    {
        var now = DateTimeOffset.UtcNow;
        return new Preset(Preset.CurrentSchemaVersion, Guid.NewGuid(), "Desk", assignments, now, now);
    }

    private static MonitorSnapshot CreateMonitor(
        string key,
        int width,
        int height,
        WallpaperSource source)
    {
        var identity = new MonitorIdentity(key, key, 1, width, height, 0, 0);
        return new MonitorSnapshot(identity, "Monitor 1", source);
    }

    private static (int Width, int Height) ReadPngSize(string path)
    {
        using var file = File.OpenRead(path);
        var header = new byte[24];
        var read = file.Read(header, 0, header.Length);
        Assert.Equal(header.Length, read);
        Assert.Equal(137, header[0]);
        Assert.Equal((byte)'P', header[1]);
        Assert.Equal((byte)'N', header[2]);
        Assert.Equal((byte)'G', header[3]);

        return (ReadBigEndian(header, 16), ReadBigEndian(header, 20));
    }

    private static async Task WriteTwoColorSourceAsync(string path)
    {
        var pixels = new PixelBuffer(2, 1, new byte[6]);
        pixels.SetPixel(0, 0, new RgbColor(255, 0, 0));
        pixels.SetPixel(1, 0, new RgbColor(0, 255, 0));
        await SolidColorPngWriter.WriteAsync(path, pixels);
    }

    private static async Task WriteFourColorSourceAsync(string path)
    {
        var pixels = new PixelBuffer(4, 1, new byte[12]);
        pixels.SetPixel(0, 0, new RgbColor(255, 0, 0));
        pixels.SetPixel(1, 0, new RgbColor(0, 255, 0));
        pixels.SetPixel(2, 0, new RgbColor(0, 0, 255));
        pixels.SetPixel(3, 0, new RgbColor(255, 255, 255));
        await SolidColorPngWriter.WriteAsync(path, pixels);
    }

    private static PixelBuffer ReadPngPixels(string path)
    {
        var bytes = File.ReadAllBytes(path);
        Assert.Equal(137, bytes[0]);
        Assert.Equal((byte)'P', bytes[1]);
        Assert.Equal((byte)'N', bytes[2]);
        Assert.Equal((byte)'G', bytes[3]);

        var width = ReadBigEndian(bytes, 16);
        var height = ReadBigEndian(bytes, 20);
        var idatOffset = FindChunk(bytes, "IDAT");
        var idatLength = ReadBigEndian(bytes, idatOffset - 4);
        var idat = bytes[(idatOffset + 4)..(idatOffset + 4 + idatLength)];
        var raw = InflateStoredZlib(idat);
        var pixels = new PixelBuffer(width, height, new byte[checked(width * height * 3)]);
        var rowLength = 1 + (width * 3);

        for (var y = 0; y < height; y++)
        {
            var rowOffset = y * rowLength;
            Assert.Equal(0, raw[rowOffset]);
            Buffer.BlockCopy(raw, rowOffset + 1, pixels.Data, y * width * 3, width * 3);
        }

        return pixels;
    }

    private static int FindChunk(byte[] bytes, string chunkName)
    {
        var chunkBytes = System.Text.Encoding.ASCII.GetBytes(chunkName);
        var offset = 8;
        while (offset < bytes.Length)
        {
            var length = ReadBigEndian(bytes, offset);
            var typeOffset = offset + 4;
            if (bytes.AsSpan(typeOffset, 4).SequenceEqual(chunkBytes))
            {
                return typeOffset;
            }

            offset += 12 + length;
        }

        throw new InvalidOperationException($"PNG chunk not found: {chunkName}");
    }

    private static byte[] InflateStoredZlib(byte[] idat)
    {
        Assert.Equal(0x78, idat[0]);
        var output = new List<byte>();
        var offset = 2;
        var final = false;

        while (!final)
        {
            var header = idat[offset++];
            final = (header & 0x01) == 1;
            Assert.Equal(0, header & 0x06);

            var length = idat[offset] | (idat[offset + 1] << 8);
            offset += 2;
            var complement = idat[offset] | (idat[offset + 1] << 8);
            offset += 2;
            Assert.Equal((ushort)~length, (ushort)complement);

            output.AddRange(idat.AsSpan(offset, length).ToArray());
            offset += length;
        }

        return output.ToArray();
    }

    private static int ReadBigEndian(byte[] buffer, int offset)
    {
        return (buffer[offset] << 24)
            | (buffer[offset + 1] << 16)
            | (buffer[offset + 2] << 8)
            | buffer[offset + 3];
    }

    private static WallpaperApplyService CreateApplyService(
        string root,
        RecordingWallpaperApplier applier)
    {
        return new WallpaperApplyService(
            new BasicPngWallpaperRenderer(new RenderedWallpaperStore(root)),
            applier);
    }

    private sealed class RecordingWallpaperApplier(
        bool succeed,
        string errorCode = ApplyErrorCodes.WallpaperApplyFailed,
        string errorMessage = "Fake failure.") : IWallpaperApplier
    {
        public RenderedWallpaper? LastWallpaper { get; private set; }

        public Task<ApplyResult> ApplyAsync(
            RenderedWallpaper wallpaper,
            CancellationToken cancellationToken = default)
        {
            LastWallpaper = wallpaper;
            return Task.FromResult(succeed
                ? ApplyResult.Success(wallpaper.Monitor)
                : ApplyResult.Failure(wallpaper.Monitor, errorCode, errorMessage));
        }
    }

    private sealed class CancelingWallpaperRenderer : IWallpaperRenderer
    {
        public CancellationTokenSource? Cancellation { get; set; }

        public Task<RenderedWallpaper> RenderAsync(
            RenderRequest request,
            CancellationToken cancellationToken = default)
        {
            Cancellation?.Cancel();
            cancellationToken.ThrowIfCancellationRequested();
            throw new OperationCanceledException(cancellationToken);
        }
    }

    private sealed class ThrowingWallpaperRenderer(Exception error) : IWallpaperRenderer
    {
        public Task<RenderedWallpaper> RenderAsync(
            RenderRequest request,
            CancellationToken cancellationToken = default)
        {
            throw error;
        }
    }

    private sealed class PassthroughWallpaperRenderer : IWallpaperRenderer
    {
        public Task<RenderedWallpaper> RenderAsync(
            RenderRequest request,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(new RenderedWallpaper(
                request.Monitor.Identity,
                $@"C:\rendered\{request.Monitor.Identity.MonitorKey}.png",
                request.Monitor.Bounds.Width,
                request.Monitor.Bounds.Height,
                DateTimeOffset.UtcNow));
        }
    }

    private sealed class CancelOnSecondWallpaperApplier : IWallpaperApplier
    {
        private int count;

        public CancellationTokenSource? Cancellation { get; set; }

        public Task<ApplyResult> ApplyAsync(
            RenderedWallpaper wallpaper,
            CancellationToken cancellationToken = default)
        {
            count++;
            if (count == 2)
            {
                Cancellation?.Cancel();
                cancellationToken.ThrowIfCancellationRequested();
            }

            return Task.FromResult(ApplyResult.Success(wallpaper.Monitor));
        }
    }

    private sealed class RecordingDesktopWallpaperWriter : IDesktopWallpaperWriter
    {
        public string? MonitorId { get; private set; }

        public string? WallpaperPath { get; private set; }

        public DesktopWallpaperPosition? Position { get; private set; }

        public void SetWallpaper(
            string monitorId,
            string wallpaperPath,
            DesktopWallpaperPosition position)
        {
            MonitorId = monitorId;
            WallpaperPath = wallpaperPath;
            Position = position;
        }
    }

    private sealed class RecordingDesktopWallpaperCom : IDesktopWallpaper
    {
        public List<string> Calls { get; } = [];

        public int SetWallpaper(string? monitorId, string wallpaper)
        {
            Calls.Add($"SetWallpaper:{monitorId}:{wallpaper}");
            return 0;
        }

        public int GetWallpaper(string? monitorId, out IntPtr wallpaper)
        {
            wallpaper = IntPtr.Zero;
            return 0;
        }

        public int GetMonitorDevicePathAt(uint monitorIndex, out IntPtr monitorId)
        {
            monitorId = IntPtr.Zero;
            return 0;
        }

        public int GetMonitorDevicePathCount(out uint count)
        {
            count = 0;
            return 0;
        }

        public int GetMonitorRECT(string monitorId, out DesktopWallpaperRect displayRect)
        {
            displayRect = default;
            return 0;
        }

        public int SetBackgroundColor(uint color) => 0;

        public int GetBackgroundColor(out uint color)
        {
            color = 0;
            return 0;
        }

        public int SetPosition(DesktopWallpaperPosition position)
        {
            Calls.Add($"SetPosition:{position}");
            return 0;
        }

        public int GetPosition(out DesktopWallpaperPosition position)
        {
            position = DesktopWallpaperPosition.Fill;
            return 0;
        }

        public int SetSlideshow(IntPtr items) => 0;

        public int GetSlideshow(out IntPtr items)
        {
            items = IntPtr.Zero;
            return 0;
        }

        public int SetSlideshowOptions(DesktopSlideshowOptions options, uint slideshowTick) => 0;

        public int GetSlideshowOptions(out DesktopSlideshowOptions options, out uint slideshowTick)
        {
            options = default;
            slideshowTick = 0;
            return 0;
        }

        public int AdvanceSlideshow(string? monitorId, DesktopSlideshowDirection direction) => 0;

        public int GetStatus(out DesktopSlideshowStatus state)
        {
            state = default;
            return 0;
        }

        public int Enable(bool enable) => 0;
    }

    private sealed class ThrowingDesktopWallpaperWriter(Exception error) : IDesktopWallpaperWriter
    {
        public void SetWallpaper(
            string monitorId,
            string wallpaperPath,
            DesktopWallpaperPosition position)
        {
            throw error;
        }
    }

    private sealed class FixedDesktopWallpaperReader(
        WallpaperPlacement currentPlacement,
        WallpaperSource backgroundSource,
        IReadOnlyList<DesktopWallpaperSnapshot> monitors) : IDesktopWallpaperReader
    {
        public WallpaperPlacement CurrentPlacement => currentPlacement;

        public WallpaperSource BackgroundSource => backgroundSource;

        public IReadOnlyList<DesktopWallpaperSnapshot> ReadMonitors(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return monitors;
        }
    }

    private sealed class FixedMonitorDetector(IReadOnlyList<MonitorSnapshot> monitors) : IMonitorDetector
    {
        public Task<IReadOnlyList<MonitorSnapshot>> DetectAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(monitors);
        }
    }

}
