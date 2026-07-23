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
    public void ActiveSessionFactory_RejectsNullMonitorDetector()
    {
        IMonitorDetector? monitorDetector = null;

        var error = Assert.Throws<ArgumentNullException>(() => new ActiveSessionFactory(monitorDetector!));

        Assert.Equal("monitorDetector", error.ParamName);
    }

    [Fact]
    public async Task CurrentSessionLoader_UsesPrimarySessionWhenDetected()
    {
        var fallbackMonitor = CreateMonitor("DISPLAY-FALLBACK", 16, 16, WallpaperSource.Empty);

        var result = await CurrentSessionLoader.LoadAsync(
            new SampleMonitorDetector(),
            new FixedMonitorDetector([fallbackMonitor]));

        Assert.False(result.UsedFallback);
        Assert.Equal(3, result.Session.Monitors.Count);
        Assert.DoesNotContain(result.Session.Monitors, monitor => monitor.Monitor.Identity.MonitorKey == fallbackMonitor.Identity.MonitorKey);
    }

    [Fact]
    public async Task CurrentSessionLoader_UsesFallbackWhenPrimaryIsEmpty()
    {
        var fallbackMonitor = CreateMonitor("DISPLAY-FALLBACK", 16, 16, WallpaperSource.Empty);

        var result = await CurrentSessionLoader.LoadAsync(
            new EmptyMonitorDetector(),
            new FixedMonitorDetector([fallbackMonitor]));

        Assert.True(result.UsedFallback);
        var monitor = Assert.Single(result.Session.Monitors);
        Assert.Equal(fallbackMonitor.Identity.MonitorKey, monitor.Monitor.Identity.MonitorKey);
    }

    [Fact]
    public async Task CurrentSessionLoader_UsesFallbackWhenPrimaryFails()
    {
        var fallbackMonitor = CreateMonitor("DISPLAY-FALLBACK", 16, 16, WallpaperSource.Empty);

        var result = await CurrentSessionLoader.LoadAsync(
            new ThrowingMonitorDetector(new InvalidOperationException("desktop unavailable")),
            new FixedMonitorDetector([fallbackMonitor]));

        Assert.True(result.UsedFallback);
        var monitor = Assert.Single(result.Session.Monitors);
        Assert.Equal(fallbackMonitor.Identity.MonitorKey, monitor.Monitor.Identity.MonitorKey);
    }

    [Fact]
    public async Task CurrentSessionLoader_PropagatesCancellationInsteadOfFallback()
    {
        var fallbackMonitor = CreateMonitor("DISPLAY-FALLBACK", 16, 16, WallpaperSource.Empty);

        await Assert.ThrowsAsync<OperationCanceledException>(() => CurrentSessionLoader.LoadAsync(
            new ThrowingMonitorDetector(new OperationCanceledException()),
            new FixedMonitorDetector([fallbackMonitor])));
    }

    [Fact]
    public async Task CurrentSessionLoader_RejectsNullPrimaryDetector()
    {
        IMonitorDetector? primaryMonitorDetector = null;

        var error = await Assert.ThrowsAsync<ArgumentNullException>(() => CurrentSessionLoader.LoadAsync(
            primaryMonitorDetector!,
            new EmptyMonitorDetector()));

        Assert.Equal("primaryMonitorDetector", error.ParamName);
    }

    [Fact]
    public async Task CurrentSessionLoader_RejectsNullFallbackDetector()
    {
        IMonitorDetector? fallbackMonitorDetector = null;

        var error = await Assert.ThrowsAsync<ArgumentNullException>(() => CurrentSessionLoader.LoadAsync(
            new EmptyMonitorDetector(),
            fallbackMonitorDetector!));

        Assert.Equal("fallbackMonitorDetector", error.ParamName);
    }

    [Fact]
    public void CurrentSessionLoadResult_RejectsNullSession()
    {
        ActiveSession? session = null;

        var error = Assert.Throws<ArgumentNullException>(() => new CurrentSessionLoadResult(
            session!,
            UsedFallback: false));

        Assert.Equal("Session", error.ParamName);
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
    public void ActiveSession_FromMonitorsRejectsNullMonitorList()
    {
        IReadOnlyList<MonitorSnapshot>? monitors = null;

        var error = Assert.Throws<ArgumentNullException>(() => ActiveSession.FromMonitors(monitors!));

        Assert.Equal("monitors", error.ParamName);
    }

    [Fact]
    public void ActiveSession_FromMonitorsRejectsNullMonitorItems()
    {
        var error = Assert.Throws<ArgumentException>(() =>
            ActiveSession.FromMonitors([null!]));

        Assert.Equal("monitors", error.ParamName);
        Assert.Contains("Active Session monitor snapshot list cannot include null items.", error.Message);
    }

    [Fact]
    public void ActiveSession_RejectsNullSessionMonitors()
    {
        IReadOnlyList<MonitorSession>? monitors = null;

        var error = Assert.Throws<ArgumentNullException>(() => new ActiveSession(
            monitors!,
            null,
            HasUnsavedPresetChanges: false,
            []));

        Assert.Equal("Monitors", error.ParamName);
    }

    [Fact]
    public void ActiveSession_RejectsNullSessionMonitorItems()
    {
        var error = Assert.Throws<ArgumentException>(() => new ActiveSession(
            [null!],
            null,
            HasUnsavedPresetChanges: false,
            []));

        Assert.Equal("Monitors", error.ParamName);
        Assert.Contains("Active Session monitor list cannot include null items.", error.Message);
    }

    [Fact]
    public void ActiveSession_RejectsNullMissingAssignments()
    {
        IReadOnlyList<PresetAssignment>? missingAssignments = null;

        var error = Assert.Throws<ArgumentNullException>(() => new ActiveSession(
            [],
            null,
            HasUnsavedPresetChanges: false,
            missingAssignments!));

        Assert.Equal("MissingAssignments", error.ParamName);
    }

    [Fact]
    public void ActiveSession_RejectsNullMissingAssignmentItems()
    {
        var error = Assert.Throws<ArgumentException>(() => new ActiveSession(
            [],
            null,
            HasUnsavedPresetChanges: false,
            [null!]));

        Assert.Equal("MissingAssignments", error.ParamName);
        Assert.Contains("Active Session missing assignment list cannot include null items.", error.Message);
    }

    [Fact]
    public void ActiveSession_CopiesSessionCollections()
    {
        var monitor = MonitorSession.FromMonitor(CreateMonitor("DISPLAY-1", 16, 16, WallpaperSource.Empty));
        var missing = new PresetAssignment(
            new MonitorIdentity("MISSING", "Disconnected", 4, 3840, 2160, 0, 0),
            WallpaperSource.Empty,
            WallpaperPlacement.Default);
        var monitors = new List<MonitorSession> { monitor };
        var missingAssignments = new List<PresetAssignment> { missing };

        var session = new ActiveSession(
            monitors,
            null,
            HasUnsavedPresetChanges: false,
            missingAssignments);
        monitors.Clear();
        missingAssignments.Clear();

        Assert.Single(session.Monitors);
        Assert.Single(session.MissingAssignments);
    }

    [Fact]
    public async Task ActiveSession_WithExpressionRejectsNullSessionMonitors()
    {
        var session = await new ActiveSessionFactory(new SampleMonitorDetector())
            .CreateFromCurrentWindowsStateAsync();
        IReadOnlyList<MonitorSession>? monitors = null;

        var error = Assert.Throws<ArgumentNullException>(() => session with { Monitors = monitors! });

        Assert.Equal("value", error.ParamName);
    }

    [Fact]
    public async Task ActiveSession_WithExpressionRejectsNullSessionMonitorItems()
    {
        var session = await new ActiveSessionFactory(new SampleMonitorDetector())
            .CreateFromCurrentWindowsStateAsync();

        var error = Assert.Throws<ArgumentException>(() => session with { Monitors = [null!] });

        Assert.Equal("value", error.ParamName);
        Assert.Contains("Active Session monitor list cannot include null items.", error.Message);
    }

    [Fact]
    public async Task ActiveSession_WithExpressionRejectsNullMissingAssignments()
    {
        var session = await new ActiveSessionFactory(new SampleMonitorDetector())
            .CreateFromCurrentWindowsStateAsync();
        IReadOnlyList<PresetAssignment>? missingAssignments = null;

        var error = Assert.Throws<ArgumentNullException>(() => session with
        {
            MissingAssignments = missingAssignments!,
        });

        Assert.Equal("value", error.ParamName);
    }

    [Fact]
    public async Task ActiveSession_WithExpressionRejectsNullMissingAssignmentItems()
    {
        var session = await new ActiveSessionFactory(new SampleMonitorDetector())
            .CreateFromCurrentWindowsStateAsync();

        var error = Assert.Throws<ArgumentException>(() => session with
        {
            MissingAssignments = [null!],
        });

        Assert.Equal("value", error.ParamName);
        Assert.Contains("Active Session missing assignment list cannot include null items.", error.Message);
    }

    [Fact]
    public async Task ActiveSession_WithSavedPresetRejectsNullPreset()
    {
        var session = await new ActiveSessionFactory(new SampleMonitorDetector())
            .CreateFromCurrentWindowsStateAsync();
        PresetIdentity? preset = null;

        var error = Assert.Throws<ArgumentNullException>(() => session.WithSavedPreset(preset!));

        Assert.Equal("preset", error.ParamName);
    }

    [Fact]
    public void MonitorKeys_CreateSetUsesCaseInsensitiveComparer()
    {
        var set = MonitorKeys.CreateSet(["DISPLAY-1"]);

        Assert.Contains("display-1", set);
    }

    [Fact]
    public void MonitorKeys_ContainsIgnoresSourceSetComparer()
    {
        IReadOnlySet<string> monitorKeys = new HashSet<string>(StringComparer.Ordinal)
        {
            "DISPLAY-1",
        };

        Assert.True(MonitorKeys.Contains(monitorKeys, "display-1"));
    }

    [Fact]
    public void MonitorKeys_RequireReturnsValidMonitorKey()
    {
        Assert.Equal("DISPLAY-1", MonitorKeys.Require("DISPLAY-1", "monitorKey"));
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void MonitorKeys_CreateSetRejectsBlankSingleMonitorKey(string monitorKey)
    {
        var error = Assert.Throws<ArgumentException>(() => MonitorKeys.CreateSet(monitorKey));

        Assert.Equal("monitorKey", error.ParamName);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void MonitorKeys_RequireRejectsBlankMonitorKey(string monitorKey)
    {
        var error = Assert.Throws<ArgumentException>(() => MonitorKeys.Require(monitorKey, "monitorKey"));

        Assert.Equal("monitorKey", error.ParamName);
        Assert.Contains("Monitor key is required.", error.Message);
    }

    [Fact]
    public void MonitorKeys_CreateSetRejectsNullMonitorKeyEnumerable()
    {
        IEnumerable<string>? monitorKeys = null;

        var error = Assert.Throws<ArgumentNullException>(() => MonitorKeys.CreateSet(monitorKeys!));

        Assert.Equal("monitorKeys", error.ParamName);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void MonitorKeys_CreateSetRejectsBlankEnumerableMonitorKey(string monitorKey)
    {
        var error = Assert.Throws<ArgumentException>(() => MonitorKeys.CreateSet(["DISPLAY-1", monitorKey]));

        Assert.Equal("monitorKeys", error.ParamName);
    }

    [Fact]
    public void MonitorSession_FromMonitorRejectsNullMonitor()
    {
        MonitorSnapshot? monitor = null;

        var error = Assert.Throws<ArgumentNullException>(() => MonitorSession.FromMonitor(monitor!));

        Assert.Equal("monitor", error.ParamName);
    }

    [Fact]
    public void MonitorSession_RejectsNullMonitor()
    {
        MonitorSnapshot? monitor = null;
        var assignment = new PresetAssignment(
            new MonitorIdentity("DISPLAY-1", "Monitor 1", 1, 16, 16, 0, 0),
            WallpaperSource.Empty,
            WallpaperPlacement.Default);

        var error = Assert.Throws<ArgumentNullException>(() => new MonitorSession(
            monitor!,
            assignment,
            assignment,
            MonitorApplyStatus.Clean,
            null,
            HasUnsavedPresetChanges: false));

        Assert.Equal("Monitor", error.ParamName);
    }

    [Fact]
    public void MonitorSession_RejectsNullDesiredAssignment()
    {
        var monitor = CreateMonitor("DISPLAY-1", 16, 16, WallpaperSource.Empty);
        PresetAssignment? assignment = null;

        var error = Assert.Throws<ArgumentNullException>(() => new MonitorSession(
            monitor,
            assignment!,
            null,
            MonitorApplyStatus.Clean,
            null,
            HasUnsavedPresetChanges: false));

        Assert.Equal("DesiredAssignment", error.ParamName);
    }

    [Fact]
    public void MonitorSession_WithExpressionRejectsNullMonitor()
    {
        var session = MonitorSession.FromMonitor(CreateMonitor("DISPLAY-1", 16, 16, WallpaperSource.Empty));
        MonitorSnapshot? monitor = null;

        var error = Assert.Throws<ArgumentNullException>(() => session with { Monitor = monitor! });

        Assert.Equal("value", error.ParamName);
    }

    [Fact]
    public void MonitorSession_WithExpressionRejectsNullDesiredAssignment()
    {
        var session = MonitorSession.FromMonitor(CreateMonitor("DISPLAY-1", 16, 16, WallpaperSource.Empty));
        PresetAssignment? assignment = null;

        var error = Assert.Throws<ArgumentNullException>(() => session with { DesiredAssignment = assignment! });

        Assert.Equal("value", error.ParamName);
    }

    [Fact]
    public void MonitorSession_RejectsInvalidApplyStatus()
    {
        var monitor = CreateMonitor("DISPLAY-1", 16, 16, WallpaperSource.Empty);
        var assignment = new PresetAssignment(
            monitor.Identity,
            WallpaperSource.Empty,
            WallpaperPlacement.Default);

        var error = Assert.Throws<ArgumentOutOfRangeException>(() => new MonitorSession(
            monitor,
            assignment,
            assignment,
            (MonitorApplyStatus)999,
            null,
            HasUnsavedPresetChanges: false));

        Assert.Equal("value", error.ParamName);
    }

    [Fact]
    public void MonitorSession_WithExpressionRejectsInvalidApplyStatus()
    {
        var session = MonitorSession.FromMonitor(CreateMonitor("DISPLAY-1", 16, 16, WallpaperSource.Empty));

        var error = Assert.Throws<ArgumentOutOfRangeException>(() =>
            session with { ApplyStatus = (MonitorApplyStatus)999 });

        Assert.Equal("value", error.ParamName);
    }

    [Fact]
    public void MonitorSession_WithPendingAssignmentRejectsNullAssignment()
    {
        var monitor = MonitorSession.FromMonitor(CreateMonitor("DISPLAY-1", 16, 16, WallpaperSource.Empty));
        PresetAssignment? assignment = null;

        var error = Assert.Throws<ArgumentNullException>(() =>
            monitor.WithPendingAssignment(assignment!, hasUnsavedPresetChanges: true));

        Assert.Equal("assignment", error.ParamName);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void MonitorSession_WithApplyErrorRejectsBlankError(string errorCode)
    {
        var monitor = MonitorSession.FromMonitor(CreateMonitor("DISPLAY-1", 16, 16, WallpaperSource.Empty));

        var error = Assert.Throws<ArgumentException>(() => monitor.WithApplyError(errorCode));

        Assert.Equal("error", error.ParamName);
    }

    [Fact]
    public void MonitorSnapshot_RejectsNullIdentity()
    {
        MonitorIdentity? identity = null;

        var error = Assert.Throws<ArgumentNullException>(() =>
            new MonitorSnapshot(identity!, "Monitor 1", WallpaperSource.Empty));

        Assert.Equal("identity", error.ParamName);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void MonitorSnapshot_RejectsBlankDisplayName(string displayName)
    {
        var identity = new MonitorIdentity("DISPLAY-1", "Monitor 1", 1, 1920, 1080, 0, 0);

        var error = Assert.Throws<ArgumentException>(() =>
            new MonitorSnapshot(identity, displayName, WallpaperSource.Empty));

        Assert.Equal("displayName", error.ParamName);
    }

    [Fact]
    public void MonitorSnapshot_TrimsDisplayName()
    {
        var identity = new MonitorIdentity("DISPLAY-1", "Monitor 1", 1, 1920, 1080, 0, 0);

        var monitor = new MonitorSnapshot(identity, "  Monitor 1  ", WallpaperSource.Empty);

        Assert.Equal("Monitor 1", monitor.DisplayName);
    }

    [Fact]
    public void MonitorSnapshot_RejectsNullCurrentSource()
    {
        var identity = new MonitorIdentity("DISPLAY-1", "Monitor 1", 1, 1920, 1080, 0, 0);
        WallpaperSource? source = null;

        var error = Assert.Throws<ArgumentNullException>(() =>
            new MonitorSnapshot(identity, "Monitor 1", source!));

        Assert.Equal("currentSource", error.ParamName);
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

    [Theory]
    [InlineData("session")]
    [InlineData("monitorKey")]
    [InlineData("source")]
    [InlineData("placement")]
    public async Task ActiveSessionEditor_UpdateAssignmentRejectsInvalidInputs(string parameterName)
    {
        var editor = new ActiveSessionEditor();
        var session = await new ActiveSessionFactory(new SampleMonitorDetector())
            .CreateFromCurrentWindowsStateAsync();
        ActiveSession? maybeSession = parameterName == "session" ? null : session;
        var monitorKey = parameterName == "monitorKey" ? " " : session.Monitors[0].Monitor.Identity.MonitorKey;
        WallpaperSource? source = parameterName == "source" ? null : WallpaperSource.Empty;
        WallpaperPlacement? placement = parameterName == "placement" ? null : WallpaperPlacement.Default;

        var error = Assert.ThrowsAny<ArgumentException>(() =>
            editor.UpdateAssignment(maybeSession!, monitorKey, source!, placement!));

        Assert.Equal(parameterName, error.ParamName);
    }

    [Theory]
    [InlineData("session")]
    [InlineData("monitorKey")]
    public async Task ActiveSessionEditor_RemoveMissingAssignmentRejectsInvalidInputs(string parameterName)
    {
        var editor = new ActiveSessionEditor();
        var session = await new ActiveSessionFactory(new SampleMonitorDetector())
            .CreateFromCurrentWindowsStateAsync();
        ActiveSession? maybeSession = parameterName == "session" ? null : session;
        var monitorKey = parameterName == "monitorKey" ? " " : "MISSING";

        var error = Assert.ThrowsAny<ArgumentException>(() =>
            editor.RemoveMissingAssignment(maybeSession!, monitorKey));

        Assert.Equal(parameterName, error.ParamName);
    }

    [Theory]
    [InlineData("session")]
    [InlineData("missingMonitorKey")]
    [InlineData("targetMonitorKey")]
    public async Task ActiveSessionEditor_ReassignMissingAssignmentRejectsInvalidInputs(string parameterName)
    {
        var editor = new ActiveSessionEditor();
        var session = await new ActiveSessionFactory(new SampleMonitorDetector())
            .CreateFromCurrentWindowsStateAsync();
        ActiveSession? maybeSession = parameterName == "session" ? null : session;
        var missingMonitorKey = parameterName == "missingMonitorKey" ? " " : "MISSING";
        var targetMonitorKey = parameterName == "targetMonitorKey" ? " " : "DISPLAY-1";

        var error = Assert.ThrowsAny<ArgumentException>(() =>
            editor.ReassignMissingAssignment(maybeSession!, missingMonitorKey, targetMonitorKey));

        Assert.Equal(parameterName, error.ParamName);
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
    public void ApplyPreflight_SkipMissingImageSourcesRejectsNullSession()
    {
        ActiveSession? session = null;

        var error = Assert.Throws<ArgumentNullException>(() =>
            ApplyPreflight.SkipMissingImageSources(session!));

        Assert.Equal("session", error.ParamName);
    }

    [Fact]
    public void ApplyPreflight_SkipMissingImageSourceRejectsNullSession()
    {
        ActiveSession? session = null;

        var error = Assert.Throws<ArgumentNullException>(() =>
            ApplyPreflight.SkipMissingImageSource(session!, "DISPLAY-1"));

        Assert.Equal("session", error.ParamName);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void ApplyPreflight_SkipMissingImageSourceRejectsBlankMonitorKey(string monitorKey)
    {
        var session = ActiveSession.FromMonitors([
            CreateMonitor("DISPLAY-1", 16, 16, WallpaperSource.FromSolidColor("#112233")),
        ]);

        var error = Assert.Throws<ArgumentException>(() =>
            ApplyPreflight.SkipMissingImageSource(session, monitorKey));

        Assert.Equal("monitorKey", error.ParamName);
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
    public void ApplyPreflightResult_ConstructorNormalizesAndCopiesMonitorKeySets()
    {
        var session = ActiveSession.FromMonitors([
            CreateMonitor("DISPLAY-1", 16, 16, WallpaperSource.Empty),
        ]);
        var readyMonitorKeys = new HashSet<string> { "display-1" };
        var skippedMonitorKeys = new HashSet<string> { "display-2" };

        var result = new ApplyPreflightResult(session, readyMonitorKeys, skippedMonitorKeys);
        readyMonitorKeys.Add("DISPLAY-3");
        skippedMonitorKeys.Clear();

        Assert.True(result.ReadyMonitorKeys.Contains("DISPLAY-1"));
        Assert.False(result.ReadyMonitorKeys.Contains("DISPLAY-3"));
        Assert.True(result.SkippedMonitorKeys.Contains("DISPLAY-2"));
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

    [Theory]
    [InlineData("session")]
    [InlineData("readyMonitorKeys")]
    [InlineData("skippedMonitorKeys")]
    public void ApplyPreflightResult_RejectsNullContractValues(string parameterName)
    {
        var session = ActiveSession.FromMonitors([
            CreateMonitor("DISPLAY-1", 16, 16, WallpaperSource.Empty),
        ]);
        var readyMonitorKeys = MonitorKeys.CreateSet();
        var skippedMonitorKeys = MonitorKeys.CreateSet();

        ActiveSession? maybeSession = parameterName == "session" ? null : session;
        IReadOnlySet<string>? maybeReadyKeys = parameterName == "readyMonitorKeys" ? null : readyMonitorKeys;
        IReadOnlySet<string>? maybeSkippedKeys = parameterName == "skippedMonitorKeys" ? null : skippedMonitorKeys;

        var error = Assert.Throws<ArgumentNullException>(() =>
            new ApplyPreflightResult(maybeSession!, maybeReadyKeys!, maybeSkippedKeys!));

        Assert.Equal(parameterName, error.ParamName);
    }

    [Fact]
    public void ApplyPreflightResult_RejectsOverlappingReadyAndSkippedKeys()
    {
        var session = ActiveSession.FromMonitors([
            CreateMonitor("DISPLAY-1", 16, 16, WallpaperSource.Empty),
        ]);

        var error = Assert.Throws<ArgumentException>(() =>
            ApplyPreflightResult.FromSets(
                session,
                readyMonitorKeys: ["DISPLAY-1"],
                skippedMonitorKeys: ["display-1"]));

        Assert.Equal("skippedMonitorKeys", error.ParamName);
        Assert.Contains("Apply preflight cannot mark a monitor as both ready and skipped.", error.Message);
    }

    [Fact]
    public void ApplyPreflightResult_WithSessionPreservesKeySets()
    {
        var originalSession = ActiveSession.FromMonitors([
            CreateMonitor("DISPLAY-1", 16, 16, WallpaperSource.Empty),
        ]);
        var nextSession = ActiveSession.FromMonitors([
            CreateMonitor("DISPLAY-2", 16, 16, WallpaperSource.Empty),
        ]);
        var result = ApplyPreflightResult.FromSets(
            originalSession,
            readyMonitorKeys: ["DISPLAY-1"],
            skippedMonitorKeys: ["DISPLAY-3"]);

        var next = result.WithSession(nextSession);

        Assert.Same(nextSession, next.Session);
        Assert.Contains("DISPLAY-1", next.ReadyMonitorKeys);
        Assert.Contains("DISPLAY-3", next.SkippedMonitorKeys);
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

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void ApplyTargetPlan_MonitorRejectsBlankMonitorKey(string monitorKey)
    {
        var error = Assert.Throws<ArgumentException>(() => ApplyTargetPlan.Monitor(monitorKey));

        Assert.Equal("monitorKey", error.ParamName);
    }

    [Fact]
    public void ApplyTargetPlan_ReadyKeysRejectsNullKeySet()
    {
        IReadOnlySet<string>? monitorKeys = null;

        var error = Assert.Throws<ArgumentNullException>(() => ApplyTargetPlan.ReadyKeys(monitorKeys!));

        Assert.Equal("monitorKeys", error.ParamName);
    }

    [Fact]
    public void ApplyTargetPlan_IncludesRejectsNullMonitor()
    {
        MonitorSession? monitor = null;
        var plan = ApplyTargetPlan.All;

        var error = Assert.Throws<ArgumentNullException>(() => plan.Includes(monitor!));

        Assert.Equal("monitor", error.ParamName);
    }

    [Fact]
    public void ApplyTargetPlan_CountRejectsNullMonitorList()
    {
        IReadOnlyList<MonitorSession>? monitors = null;
        var plan = ApplyTargetPlan.All;

        var error = Assert.Throws<ArgumentNullException>(() => plan.Count(monitors!));

        Assert.Equal("monitors", error.ParamName);
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

    [Theory]
    [InlineData("Width")]
    [InlineData("Height")]
    public void MonitorBounds_RejectsNonPositiveDimensions(string parameterName)
    {
        var width = parameterName == "Width" ? 0 : 1920;
        var height = parameterName == "Height" ? 0 : 1080;

        var error = Assert.Throws<ArgumentOutOfRangeException>(() =>
            new MonitorBounds(0, 0, width, height));

        Assert.Equal(parameterName, error.ParamName);
    }

    [Theory]
    [InlineData("Width")]
    [InlineData("Height")]
    public void MonitorBounds_WithExpressionRejectsNonPositiveDimensions(string parameterName)
    {
        var bounds = new MonitorBounds(0, 0, 1920, 1080);

        var error = Assert.Throws<ArgumentOutOfRangeException>(() => parameterName == "Width"
            ? bounds with { Width = 0 }
            : bounds with { Height = 0 });

        Assert.Equal("value", error.ParamName);
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

    [Theory]
    [InlineData("SurfaceWidth")]
    [InlineData("SurfaceHeight")]
    [InlineData("Scale")]
    public void MonitorTopologyLayout_RejectsInvalidDirectValues(string parameterName)
    {
        double surfaceWidth = parameterName == "SurfaceWidth" ? 0 : 720;
        double surfaceHeight = parameterName == "SurfaceHeight" ? 0 : 96;
        double scale = parameterName == "Scale" ? 0 : 1;

        var error = Assert.Throws<ArgumentOutOfRangeException>(() =>
            new MonitorTopologyLayout(surfaceWidth, surfaceHeight, 0, 0, scale));

        Assert.Equal(parameterName, error.ParamName);
    }

    [Theory]
    [InlineData("SurfaceWidth")]
    [InlineData("SurfaceHeight")]
    [InlineData("Scale")]
    public void MonitorTopologyLayout_WithExpressionRejectsInvalidValues(string propertyName)
    {
        var layout = new MonitorTopologyLayout(720, 96, 0, 0, 1);

        var error = Assert.Throws<ArgumentOutOfRangeException>(() => propertyName switch
        {
            "SurfaceWidth" => layout with { SurfaceWidth = 0 },
            "SurfaceHeight" => layout with { SurfaceHeight = 0 },
            _ => layout with { Scale = 0 },
        });

        Assert.Equal("value", error.ParamName);
    }

    [Fact]
    public void MonitorTopologyLayout_RejectsNullBoundsList()
    {
        IReadOnlyList<MonitorBounds>? bounds = null;

        var error = Assert.Throws<ArgumentNullException>(() => MonitorTopologyLayout.Calculate(bounds!));

        Assert.Equal("bounds", error.ParamName);
    }

    [Theory]
    [InlineData("maxWidth")]
    [InlineData("maxHeight")]
    [InlineData("minSurfaceWidth")]
    [InlineData("minSurfaceHeight")]
    public void MonitorTopologyLayout_RejectsInvalidSurfaceDimensions(string parameterName)
    {
        var bounds = new[] { new MonitorBounds(0, 0, 1920, 1080) };
        double maxWidth = parameterName == "maxWidth" ? 0 : 720;
        double maxHeight = parameterName == "maxHeight" ? 0 : 96;
        double minSurfaceWidth = parameterName == "minSurfaceWidth" ? 0 : 96;
        double minSurfaceHeight = parameterName == "minSurfaceHeight" ? 0 : 48;

        var error = Assert.Throws<ArgumentOutOfRangeException>(() => MonitorTopologyLayout.Calculate(
            bounds,
            maxWidth,
            maxHeight,
            minSurfaceWidth,
            minSurfaceHeight));

        Assert.Equal(parameterName, error.ParamName);
    }

    [Fact]
    public void MonitorTopologyLayout_TileForRejectsNullBounds()
    {
        var layout = MonitorTopologyLayout.Calculate([]);
        MonitorBounds? bounds = null;

        var error = Assert.Throws<ArgumentNullException>(() => layout.TileFor(bounds!));

        Assert.Equal("bounds", error.ParamName);
    }

    [Theory]
    [InlineData("minTileWidth")]
    [InlineData("minTileHeight")]
    public void MonitorTopologyLayout_TileForRejectsInvalidTileDimensions(string parameterName)
    {
        var layout = MonitorTopologyLayout.Calculate([]);
        var bounds = new MonitorBounds(0, 0, 1920, 1080);
        double minTileWidth = parameterName == "minTileWidth" ? 0 : 48;
        double minTileHeight = parameterName == "minTileHeight" ? 0 : 28;

        var error = Assert.Throws<ArgumentOutOfRangeException>(() => layout.TileFor(
            bounds,
            minTileWidth,
            minTileHeight));

        Assert.Equal(parameterName, error.ParamName);
    }

    [Theory]
    [InlineData("Width")]
    [InlineData("Height")]
    public void MonitorTopologyTile_RejectsInvalidDirectDimensions(string parameterName)
    {
        double width = parameterName == "Width" ? 0 : 48;
        double height = parameterName == "Height" ? 0 : 28;

        var error = Assert.Throws<ArgumentOutOfRangeException>(() =>
            new MonitorTopologyTile(0, 0, width, height));

        Assert.Equal(parameterName, error.ParamName);
    }

    [Theory]
    [InlineData("Width")]
    [InlineData("Height")]
    public void MonitorTopologyTile_WithExpressionRejectsInvalidDimensions(string propertyName)
    {
        var tile = new MonitorTopologyTile(0, 0, 48, 28);

        var error = Assert.Throws<ArgumentOutOfRangeException>(() => propertyName == "Width"
            ? tile with { Width = 0 }
            : tile with { Height = 0 });

        Assert.Equal("value", error.ParamName);
    }

    [Fact]
    public void PresetNames_ValidatesAndTrimsNames()
    {
        Assert.Equal("Desk", PresetNames.Validate("  Desk  "));
        Assert.Throws<ArgumentNullException>(() => PresetNames.Validate(null!));
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
    public void PresetNames_UsesCallerParameterName()
    {
        var error = Assert.Throws<ArgumentException>(() => PresetNames.Validate(" ", "PresetName"));

        Assert.Equal("PresetName", error.ParamName);
    }

    [Fact]
    public void PresetIds_ValidatesRealPresetIds()
    {
        var id = Guid.NewGuid();

        Assert.True(PresetIds.IsValid(id));
        Assert.False(PresetIds.IsValid(Guid.Empty));
        Assert.Equal(id, PresetIds.NormalizeOptional(id));
        Assert.Null(PresetIds.NormalizeOptional(Guid.Empty));
        Assert.Null(PresetIds.NormalizeOptional(null));
        Assert.Equal(id, PresetIds.RequireValid(id, "presetId"));

        var error = Assert.Throws<ArgumentException>(() => PresetIds.RequireValid(Guid.Empty, "presetId"));
        Assert.Equal("presetId", error.ParamName);
        Assert.Contains("Preset id cannot be empty.", error.Message);
    }

    [Fact]
    public void DefinedEnumValue_ValidatesSupportedEnumValues()
    {
        Assert.True(DefinedEnumValue.IsDefined(WallpaperSourceKind.Empty));
        Assert.False(DefinedEnumValue.IsDefined((WallpaperSourceKind)999));
        Assert.Equal(
            WallpaperSourceKind.Image,
            DefinedEnumValue.Require(
                WallpaperSourceKind.Image,
                "sourceKind",
                "Source kind is invalid."));

        var error = Assert.Throws<ArgumentOutOfRangeException>(() =>
            DefinedEnumValue.Require(
                (WallpaperSourceKind)999,
                "sourceKind",
                "Source kind is invalid."));
        Assert.Equal("sourceKind", error.ParamName);
        Assert.Contains("Source kind is invalid.", error.Message);
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

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void ColorHexValue_RejectsMissingValues(string? colorHex)
    {
        var error = Assert.Throws<ArgumentException>(() => ColorHexValue.Normalize(colorHex!));

        Assert.Equal("colorHex", error.ParamName);
        Assert.Contains("Color must be #RRGGBB.", error.Message);
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
    public void LocalDataFile_DeleteIfExistsIgnoresMissingFileOrDirectory()
    {
        var root = Path.Combine(Path.GetTempPath(), $"waller-local-file-tests-{Guid.NewGuid():N}");
        var path = Path.Combine(root, "missing", "data.json");

        LocalDataFile.DeleteIfExists(path);
        LocalDataFile.DeleteRecoverableIfExists(path);
        Assert.True(LocalDataFile.TryDeleteIfExists(path));

        Assert.False(Directory.Exists(root));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void LocalDataFile_DeleteRejectsBlankPath(string? path)
    {
        Assert.ThrowsAny<ArgumentException>(() => LocalDataFile.DeleteIfExists(path!));
        Assert.ThrowsAny<ArgumentException>(() => LocalDataFile.DeleteRecoverableIfExists(path!));
        Assert.ThrowsAny<ArgumentException>(() => LocalDataFile.TryDeleteIfExists(path!));
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

    [Theory]
    [InlineData("session")]
    [InlineData("preset")]
    public async Task PresetMatcher_ApplyPresetRejectsNullInputs(string parameterName)
    {
        var matcher = new PresetMatcher();
        var session = await new ActiveSessionFactory(new SampleMonitorDetector())
            .CreateFromCurrentWindowsStateAsync();
        var preset = CreatePreset([]);
        ActiveSession? maybeSession = parameterName == "session" ? null : session;
        Preset? maybePreset = parameterName == "preset" ? null : preset;

        var error = Assert.Throws<ArgumentNullException>(() =>
            matcher.ApplyPreset(maybeSession!, maybePreset!));

        Assert.Equal(parameterName, error.ParamName);
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

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void PresetIdentity_RejectsBlankName(string? name)
    {
        var error = Assert.ThrowsAny<ArgumentException>(() => new PresetIdentity(Guid.NewGuid(), name!));

        Assert.Equal("Name", error.ParamName);
    }

    [Fact]
    public void PresetIdentity_RejectsEmptyId()
    {
        var error = Assert.Throws<ArgumentException>(() => new PresetIdentity(Guid.Empty, "Desk"));

        Assert.Equal("Id", error.ParamName);
        Assert.Contains("Preset id cannot be empty.", error.Message);
    }

    [Fact]
    public void PresetIdentity_TrimsName()
    {
        var identity = new PresetIdentity(Guid.NewGuid(), "  Desk  ");

        Assert.Equal("Desk", identity.Name);
    }

    [Fact]
    public void PresetIdentity_WithExpressionTrimsName()
    {
        var identity = new PresetIdentity(Guid.NewGuid(), "Desk");

        var updated = identity with { Name = "  Focus  " };

        Assert.Equal("Focus", updated.Name);
    }

    [Fact]
    public void PresetIdentity_WithExpressionRejectsEmptyId()
    {
        var identity = new PresetIdentity(Guid.NewGuid(), "Desk");

        var error = Assert.Throws<ArgumentException>(() => identity with { Id = Guid.Empty });

        Assert.Equal("value", error.ParamName);
        Assert.Contains("Preset id cannot be empty.", error.Message);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void Preset_RejectsBlankName(string? name)
    {
        var error = Assert.ThrowsAny<ArgumentException>(() => new Preset(
            Preset.CurrentSchemaVersion,
            Guid.NewGuid(),
            name!,
            [],
            DateTimeOffset.UnixEpoch,
            DateTimeOffset.UnixEpoch));

        Assert.Equal("Name", error.ParamName);
    }

    [Fact]
    public void Preset_RejectsEmptyId()
    {
        var error = Assert.Throws<ArgumentException>(() => new Preset(
            Preset.CurrentSchemaVersion,
            Guid.Empty,
            "Desk",
            [],
            DateTimeOffset.UnixEpoch,
            DateTimeOffset.UnixEpoch));

        Assert.Equal("Id", error.ParamName);
        Assert.Contains("Preset id cannot be empty.", error.Message);
    }

    [Fact]
    public void Preset_TrimsName()
    {
        var preset = new Preset(
            Preset.CurrentSchemaVersion,
            Guid.NewGuid(),
            "  Desk  ",
            [],
            DateTimeOffset.UnixEpoch,
            DateTimeOffset.UnixEpoch);

        Assert.Equal("Desk", preset.Name);
    }

    [Fact]
    public void Preset_WithExpressionTrimsName()
    {
        var preset = CreatePreset([]);

        var updated = preset with { Name = "  Focus  " };

        Assert.Equal("Focus", updated.Name);
    }

    [Fact]
    public void Preset_WithExpressionRejectsEmptyId()
    {
        var preset = CreatePreset([]);

        var error = Assert.Throws<ArgumentException>(() => preset with { Id = Guid.Empty });

        Assert.Equal("value", error.ParamName);
        Assert.Contains("Preset id cannot be empty.", error.Message);
    }

    [Fact]
    public void Preset_RejectsNullAssignments()
    {
        IReadOnlyList<PresetAssignment>? assignments = null;

        var error = Assert.Throws<ArgumentNullException>(() => new Preset(
            Preset.CurrentSchemaVersion,
            Guid.NewGuid(),
            "Desk",
            assignments!,
            DateTimeOffset.UnixEpoch,
            DateTimeOffset.UnixEpoch));

        Assert.Equal("Assignments", error.ParamName);
    }

    [Fact]
    public void Preset_RejectsNullAssignmentItems()
    {
        var error = Assert.Throws<ArgumentException>(() => new Preset(
            Preset.CurrentSchemaVersion,
            Guid.NewGuid(),
            "Desk",
            [null!],
            DateTimeOffset.UnixEpoch,
            DateTimeOffset.UnixEpoch));

        Assert.Equal("Assignments", error.ParamName);
        Assert.Contains("Preset assignment list cannot include null items.", error.Message);
    }

    [Fact]
    public void Preset_CopiesAssignments()
    {
        var monitor = new MonitorIdentity("DISPLAY-1", "Monitor 1", 1, 1920, 1080, 0, 0);
        var assignment = new PresetAssignment(monitor, WallpaperSource.Empty, WallpaperPlacement.Default);
        var assignments = new List<PresetAssignment> { assignment };

        var preset = CreatePreset(assignments);
        assignments.Clear();

        Assert.Single(preset.Assignments);
    }

    [Fact]
    public void Preset_WithExpressionRejectsNullAssignments()
    {
        var preset = CreatePreset([]);
        IReadOnlyList<PresetAssignment>? assignments = null;

        var error = Assert.Throws<ArgumentNullException>(() => preset with { Assignments = assignments! });

        Assert.Equal("value", error.ParamName);
    }

    [Fact]
    public void Preset_WithExpressionRejectsNullAssignmentItems()
    {
        var preset = CreatePreset([]);

        var error = Assert.Throws<ArgumentException>(() => preset with { Assignments = [null!] });

        Assert.Equal("value", error.ParamName);
        Assert.Contains("Preset assignment list cannot include null items.", error.Message);
    }

    [Fact]
    public void PresetAssignment_RejectsNullSource()
    {
        var monitor = new MonitorIdentity("DISPLAY-1", "Monitor 1", 1, 1920, 1080, 0, 0);
        WallpaperSource? source = null;

        var error = Assert.Throws<ArgumentNullException>(() => new PresetAssignment(
            monitor,
            source!,
            WallpaperPlacement.Default));

        Assert.Equal("Source", error.ParamName);
    }

    [Fact]
    public void PresetAssignment_RejectsNullSavedMonitor()
    {
        MonitorIdentity? monitor = null;

        var error = Assert.Throws<ArgumentNullException>(() => new PresetAssignment(
            monitor!,
            WallpaperSource.Empty,
            WallpaperPlacement.Default));

        Assert.Equal("SavedMonitor", error.ParamName);
    }

    [Fact]
    public void PresetAssignment_RejectsNullPlacement()
    {
        var monitor = new MonitorIdentity("DISPLAY-1", "Monitor 1", 1, 1920, 1080, 0, 0);
        WallpaperPlacement? placement = null;

        var error = Assert.Throws<ArgumentNullException>(() => new PresetAssignment(
            monitor,
            WallpaperSource.Empty,
            placement!));

        Assert.Equal("Placement", error.ParamName);
    }

    [Fact]
    public void PresetAssignment_WithExpressionRejectsNullSource()
    {
        var monitor = new MonitorIdentity("DISPLAY-1", "Monitor 1", 1, 1920, 1080, 0, 0);
        var assignment = new PresetAssignment(monitor, WallpaperSource.Empty, WallpaperPlacement.Default);
        WallpaperSource? source = null;

        var error = Assert.Throws<ArgumentNullException>(() => assignment with { Source = source! });

        Assert.Equal("value", error.ParamName);
    }

    [Fact]
    public void PresetAssignment_WithExpressionRejectsNullSavedMonitor()
    {
        var monitor = new MonitorIdentity("DISPLAY-1", "Monitor 1", 1, 1920, 1080, 0, 0);
        var assignment = new PresetAssignment(monitor, WallpaperSource.Empty, WallpaperPlacement.Default);
        MonitorIdentity? savedMonitor = null;

        var error = Assert.Throws<ArgumentNullException>(() => assignment with { SavedMonitor = savedMonitor! });

        Assert.Equal("value", error.ParamName);
    }

    [Fact]
    public void PresetAssignment_WithExpressionRejectsNullPlacement()
    {
        var monitor = new MonitorIdentity("DISPLAY-1", "Monitor 1", 1, 1920, 1080, 0, 0);
        var assignment = new PresetAssignment(monitor, WallpaperSource.Empty, WallpaperPlacement.Default);
        WallpaperPlacement? placement = null;

        var error = Assert.Throws<ArgumentNullException>(() => assignment with { Placement = placement! });

        Assert.Equal("value", error.ParamName);
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
    public async Task PresetStore_LoadMissingPresetReturnsNullThroughRecoverableRead()
    {
        var root = Path.Combine(Path.GetTempPath(), $"waller-native-tests-{Guid.NewGuid():N}");
        try
        {
            var store = new PresetStore(root);

            var preset = await store.LoadAsync(Guid.NewGuid());

            Assert.Null(preset);
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
    public async Task PresetStore_MissingReadAndDeleteDoNotCreateLocalDataDirectory()
    {
        var root = Path.Combine(Path.GetTempPath(), $"waller-native-tests-{Guid.NewGuid():N}");
        try
        {
            var store = new PresetStore(root);

            var listed = await store.ListAsync();
            var loaded = await store.LoadAsync(Guid.NewGuid());
            await store.DeleteAsync(Guid.NewGuid());

            Assert.Empty(listed);
            Assert.Null(loaded);
            Assert.False(Directory.Exists(root));
        }
        finally
        {
            if (Directory.Exists(root))
            {
                Directory.Delete(root, recursive: true);
            }
        }
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("relative-root")]
    public void PresetStore_RejectsInvalidRootDirectory(string? rootDirectory)
    {
        var error = Assert.ThrowsAny<ArgumentException>(() => new PresetStore(rootDirectory!));

        Assert.Equal("rootDirectory", error.ParamName);
    }

    [Fact]
    public async Task PresetStore_SaveRejectsNullPreset()
    {
        var root = Path.Combine(Path.GetTempPath(), $"waller-native-tests-{Guid.NewGuid():N}");
        try
        {
            var store = new PresetStore(root);
            Preset? preset = null;

            var error = await Assert.ThrowsAsync<ArgumentNullException>(() => store.SaveAsync(preset!));

            Assert.Equal("preset", error.ParamName);
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
    public void PresetFilePolicy_NormalizeForSaveRejectsNullPreset()
    {
        Preset? preset = null;

        var error = Assert.Throws<ArgumentNullException>(() =>
            PresetFilePolicy.NormalizeForSave(preset!, DateTimeOffset.UnixEpoch));

        Assert.Equal("preset", error.ParamName);
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

    [Theory]
    [InlineData("load")]
    [InlineData("rename")]
    [InlineData("delete")]
    public async Task PresetStore_RejectsEmptyPresetId(string action)
    {
        var root = Path.Combine(Path.GetTempPath(), $"waller-native-tests-{Guid.NewGuid():N}");
        try
        {
            var store = new PresetStore(root);

            var error = await Assert.ThrowsAsync<ArgumentException>(() => action switch
            {
                "load" => store.LoadAsync(Guid.Empty),
                "rename" => store.RenameAsync(Guid.Empty, "Renamed"),
                "delete" => store.DeleteAsync(Guid.Empty),
                _ => throw new InvalidOperationException(action),
            });

            Assert.Equal("id", error.ParamName);
            Assert.Contains("Preset id cannot be empty.", error.Message);
        }
        finally
        {
            if (Directory.Exists(root))
            {
                Directory.Delete(root, recursive: true);
            }
        }
    }

    [Theory]
    [InlineData("list")]
    [InlineData("load")]
    [InlineData("save")]
    [InlineData("delete")]
    public async Task PresetStore_CancelledOperationsDoNotCreateLocalFolders(string action)
    {
        var root = Path.Combine(Path.GetTempPath(), $"waller-native-tests-{Guid.NewGuid():N}");
        try
        {
            var store = new PresetStore(root);
            using var cts = new CancellationTokenSource();
            cts.Cancel();

            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => action switch
            {
                "list" => store.ListAsync(cts.Token),
                "load" => store.LoadAsync(Guid.NewGuid(), cts.Token),
                "save" => store.SaveAsync(CreatePreset([]), cts.Token),
                "delete" => store.DeleteAsync(Guid.NewGuid(), cts.Token),
                _ => throw new InvalidOperationException(action),
            });

            Assert.False(Directory.Exists(root));
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
    public void MonitorIdentity_NullKeyBecomesInvalidPresetAssignment()
    {
        var identity = new MonitorIdentity(null!, null, 1, 1920, 1080, 0, 0);

        Assert.Equal(string.Empty, identity.MonitorKey);
        Assert.False(identity.IsValidForPresetAssignment);
    }

    [Fact]
    public void PresetAssignments_NormalizeRejectsNullAssignment()
    {
        PresetAssignment? assignment = null;

        var error = Assert.Throws<ArgumentNullException>(() => PresetAssignments.Normalize(assignment!));

        Assert.Equal("assignment", error.ParamName);
    }

    [Fact]
    public void WallpaperPlacement_RejectsInvalidFitMode()
    {
        var error = Assert.Throws<ArgumentOutOfRangeException>(() =>
            new WallpaperPlacement((WallpaperFitMode)999, WallpaperAnchor.Center));

        Assert.Equal("value", error.ParamName);
    }

    [Fact]
    public void WallpaperPlacement_RejectsInvalidAnchor()
    {
        var error = Assert.Throws<ArgumentOutOfRangeException>(() =>
            new WallpaperPlacement(WallpaperFitMode.Cover, (WallpaperAnchor)999));

        Assert.Equal("value", error.ParamName);
    }

    [Fact]
    public void WallpaperPlacement_WithExpressionRejectsInvalidFitMode()
    {
        var error = Assert.Throws<ArgumentOutOfRangeException>(() =>
            WallpaperPlacement.Default with { FitMode = (WallpaperFitMode)999 });

        Assert.Equal("value", error.ParamName);
    }

    [Fact]
    public void WallpaperPlacement_WithExpressionRejectsInvalidAnchor()
    {
        var error = Assert.Throws<ArgumentOutOfRangeException>(() =>
            WallpaperPlacement.Default with { Anchor = (WallpaperAnchor)999 });

        Assert.Equal("value", error.ParamName);
    }

    [Fact]
    public void WallpaperSource_RejectsInvalidKind()
    {
        var error = Assert.Throws<ArgumentOutOfRangeException>(() =>
            new WallpaperSource((WallpaperSourceKind)999));

        Assert.Equal("value", error.ParamName);
    }

    [Fact]
    public void WallpaperSource_WithExpressionRejectsInvalidKind()
    {
        var error = Assert.Throws<ArgumentOutOfRangeException>(() =>
            WallpaperSource.Empty with { Kind = (WallpaperSourceKind)999 });

        Assert.Equal("value", error.ParamName);
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

    [Theory]
    [InlineData("session")]
    [InlineData("identity")]
    public async Task PresetFactory_UpdateFromSessionRejectsNullInputs(string parameterName)
    {
        var session = await new ActiveSessionFactory(new SampleMonitorDetector())
            .CreateFromCurrentWindowsStateAsync();
        var identity = new PresetIdentity(Guid.NewGuid(), "Desk");
        ActiveSession? maybeSession = parameterName == "session" ? null : session;
        PresetIdentity? maybeIdentity = parameterName == "identity" ? null : identity;

        var error = Assert.Throws<ArgumentNullException>(() =>
            PresetFactory.UpdateFromSession(maybeSession!, maybeIdentity!, DateTimeOffset.UnixEpoch));

        Assert.Equal(parameterName, error.ParamName);
    }

    [Fact]
    public void PresetFactory_CreateFromSessionRejectsNullSession()
    {
        ActiveSession? session = null;

        var error = Assert.Throws<ArgumentNullException>(() =>
            PresetFactory.CreateFromSession(session!, "Desk"));

        Assert.Equal("session", error.ParamName);
    }

    [Theory]
    [InlineData("duplicate")]
    [InlineData("rename")]
    public void PresetFactory_MutationHelpersRejectNullPreset(string action)
    {
        Preset? preset = null;

        var error = action == "duplicate"
            ? Assert.Throws<ArgumentNullException>(() => PresetFactory.Duplicate(preset!, "Copy"))
            : Assert.Throws<ArgumentNullException>(() => PresetFactory.Rename(preset!, "Renamed"));

        Assert.Equal("preset", error.ParamName);
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
    public async Task UserSettingsStore_LoadMissingSettingsReturnsDefaultsThroughRecoverableRead()
    {
        var root = Path.Combine(Path.GetTempPath(), $"waller-settings-tests-{Guid.NewGuid():N}");
        try
        {
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

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("relative-root")]
    public void UserSettingsStore_RejectsInvalidRootDirectory(string? rootDirectory)
    {
        var error = Assert.ThrowsAny<ArgumentException>(() => new UserSettingsStore(rootDirectory!));

        Assert.Equal("rootDirectory", error.ParamName);
    }

    [Fact]
    public async Task UserSettingsStore_SaveRejectsNullSettings()
    {
        var root = Path.Combine(Path.GetTempPath(), $"waller-settings-tests-{Guid.NewGuid():N}");
        try
        {
            var store = new UserSettingsStore(root);
            UserSettings? settings = null;

            var error = await Assert.ThrowsAsync<ArgumentNullException>(() => store.SaveAsync(settings!));

            Assert.Equal("settings", error.ParamName);
        }
        finally
        {
            if (Directory.Exists(root))
            {
                Directory.Delete(root, recursive: true);
            }
        }
    }

    [Theory]
    [InlineData("load")]
    [InlineData("save")]
    public async Task UserSettingsStore_CancelledOperationsDoNotCreateLocalFiles(string action)
    {
        var root = Path.Combine(Path.GetTempPath(), $"waller-settings-tests-{Guid.NewGuid():N}");
        try
        {
            var store = new UserSettingsStore(root);
            using var cts = new CancellationTokenSource();
            cts.Cancel();

            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => action switch
            {
                "load" => store.LoadAsync(cts.Token),
                "save" => store.SaveAsync(UserSettings.Default, cts.Token),
                _ => throw new InvalidOperationException(action),
            });

            Assert.False(File.Exists(Path.Combine(root, "settings.json")));
            Assert.False(Directory.Exists(root));
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

            Assert.Equal(UserSettings.Default.Theme, loaded.Theme);
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
    public async Task UserSettingsStore_DropsEmptyLastSelectedPresetId()
    {
        var root = Path.Combine(Path.GetTempPath(), $"waller-settings-tests-{Guid.NewGuid():N}");
        try
        {
            var store = new UserSettingsStore(root);
            await store.SaveAsync(UserSettings.Default with { LastSelectedPresetId = Guid.Empty });

            var loaded = await store.LoadAsync();

            Assert.Null(loaded.LastSelectedPresetId);
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
    public void UserSettingsPolicy_NormalizeRejectsNullSettings()
    {
        UserSettings? settings = null;

        var error = Assert.Throws<ArgumentNullException>(() => UserSettingsPolicy.Normalize(settings!));

        Assert.Equal("settings", error.ParamName);
    }

    [Fact]
    public void UserSettings_ConvertsNullLanguageToEmptyDraftValue()
    {
        var settings = new UserSettings(
            AppThemePreference.System,
            null,
            UserSettingsPolicy.DefaultWindowWidth,
            UserSettingsPolicy.DefaultWindowHeight,
            null,
            null,
            null);

        Assert.Equal(string.Empty, settings.Language);
    }

    [Fact]
    public void UserSettings_WithExpressionConvertsNullLanguageToEmptyDraftValue()
    {
        var settings = UserSettings.Default with { Language = null! };

        Assert.Equal(string.Empty, settings.Language);
    }

    [Fact]
    public void UserSettingsPolicy_NormalizesNullLanguageToDefault()
    {
        var settings = UserSettings.Default with { Language = null! };

        var normalized = UserSettingsPolicy.Normalize(settings);

        Assert.Equal(AppLanguages.English, normalized.Language);
    }

    [Fact]
    public void UserSettings_ConvertsEmptyLastSelectedPresetIdToNull()
    {
        var settings = UserSettings.Default with { LastSelectedPresetId = Guid.Empty };

        Assert.Null(settings.LastSelectedPresetId);
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
    public void UserSettings_WithWindowPlacementClampsMinimumSize()
    {
        var updated = UserSettings.Default.WithWindowPlacement(1, 2, -20, 40);

        Assert.Equal(UserSettingsPolicy.MinWindowWidth, updated.WindowWidth);
        Assert.Equal(UserSettingsPolicy.MinWindowHeight, updated.WindowHeight);
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
    public void UserSettings_WithPreferencesNormalizesLanguage()
    {
        var updated = UserSettings.Default.WithPreferences(
            AppThemePreference.Dark,
            "ES",
            lastSelectedPresetId: null);

        Assert.Equal(AppLanguages.Spanish, updated.Language);
    }

    [Fact]
    public void UserSettings_WithPreferencesConvertsEmptyLastSelectedPresetIdToNull()
    {
        var updated = UserSettings.Default.WithPreferences(
            AppThemePreference.Dark,
            AppLanguages.Spanish,
            Guid.Empty);

        Assert.Null(updated.LastSelectedPresetId);
    }

    [Fact]
    public void UserSettings_WithPreferencesRejectsInvalidTheme()
    {
        var error = Assert.Throws<ArgumentOutOfRangeException>(() =>
            UserSettings.Default.WithPreferences(
                (AppThemePreference)999,
                AppLanguages.Spanish,
                lastSelectedPresetId: null));

        Assert.Equal("theme", error.ParamName);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("fr")]
    public void UserSettings_WithPreferencesRejectsUnsupportedLanguage(string? language)
    {
        var error = Assert.Throws<ArgumentException>(() =>
            UserSettings.Default.WithPreferences(
                AppThemePreference.Dark,
                language!,
                lastSelectedPresetId: null));

        Assert.Equal("language", error.ParamName);
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
    public void UserSettings_WithLastSelectedPresetConvertsEmptyIdToNull()
    {
        var updated = UserSettings.Default.WithLastSelectedPreset(Guid.Empty);

        Assert.Null(updated.LastSelectedPresetId);
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

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("relative-root")]
    public void RenderedWallpaperStore_RejectsInvalidRootDirectory(string? rootDirectory)
    {
        var error = Assert.ThrowsAny<ArgumentException>(() => new RenderedWallpaperStore(rootDirectory!));

        Assert.Equal("rootDirectory", error.ParamName);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void RenderedWallpaperStore_RejectsBlankMonitorKeyBeforeCreatingDirectory(string? monitorKey)
    {
        var root = Path.Combine(Path.GetTempPath(), $"waller-render-cache-tests-{Guid.NewGuid():N}");
        var store = new RenderedWallpaperStore(root);

        var error = Assert.ThrowsAny<ArgumentException>(() => store.CreatePath(monitorKey!));

        Assert.Equal("monitorKey", error.ParamName);
        Assert.False(Directory.Exists(store.RenderedDirectory));
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

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void RenderedWallpaperFileNames_CreateRejectsBlankMonitorKey(string? monitorKey)
    {
        var error = Assert.ThrowsAny<ArgumentException>(() => RenderedWallpaperFileNames.Create(
            monitorKey!,
            DateTimeOffset.UnixEpoch));

        Assert.Equal("monitorKey", error.ParamName);
    }

    [Fact]
    public void RenderedCacheClearResult_ReportsFailures()
    {
        Assert.False(new RenderedCacheClearResult(Deleted: 2, Failed: 0).HasFailures);
        Assert.True(new RenderedCacheClearResult(Deleted: 2, Failed: 1).HasFailures);
    }

    [Theory]
    [InlineData(-1, 0, "Deleted")]
    [InlineData(0, -1, "Failed")]
    public void RenderedCacheClearResult_RejectsNegativeCounts(int deleted, int failed, string parameterName)
    {
        var error = Assert.Throws<ArgumentOutOfRangeException>(() =>
            new RenderedCacheClearResult(deleted, failed));

        Assert.Equal(parameterName, error.ParamName);
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

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public async Task AtomicFileWriter_RejectsInvalidPath(string? path)
    {
        var error = await Assert.ThrowsAnyAsync<ArgumentException>(() =>
            AtomicFileWriter.WriteAsync(
                path!,
                (_, _) => Task.CompletedTask,
                CancellationToken.None));

        Assert.Equal("path", error.ParamName);
    }

    [Fact]
    public async Task AtomicFileWriter_RejectsNullWriteCallback()
    {
        Func<Stream, CancellationToken, Task>? write = null;

        var error = await Assert.ThrowsAsync<ArgumentNullException>(() =>
            AtomicFileWriter.WriteAsync("data.bin", write!, CancellationToken.None));

        Assert.Equal("write", error.ParamName);
    }

    [Fact]
    public async Task AtomicFileWriter_RejectsPathWithoutFileName()
    {
        var root = Path.Combine(Path.GetTempPath(), $"waller-atomic-write-tests-{Guid.NewGuid():N}");
        var path = root + Path.DirectorySeparatorChar;

        var error = await Assert.ThrowsAsync<ArgumentException>(() =>
            AtomicFileWriter.WriteAsync(
                path,
                (_, _) => Task.CompletedTask,
                CancellationToken.None));

        Assert.Equal("path", error.ParamName);
        Assert.Contains("Atomic write path must include a file name.", error.Message);
        Assert.False(Directory.Exists(root));
    }

    [Fact]
    public void AtomicFileWriter_CreateTempPathUsesTargetDirectoryAndFileName()
    {
        var root = Path.Combine(Path.GetTempPath(), $"waller-atomic-write-tests-{Guid.NewGuid():N}");
        var path = Path.Combine(root, "data.bin");

        var tempPath = AtomicFileWriter.CreateTempPath(path);

        Assert.Equal(root, Path.GetDirectoryName(tempPath));
        Assert.StartsWith(".data.bin.", Path.GetFileName(tempPath), StringComparison.Ordinal);
        Assert.EndsWith(".tmp", tempPath, StringComparison.Ordinal);
    }

    [Fact]
    public async Task AtomicFileWriter_CancelledWriteDoesNotCreateDirectory()
    {
        var root = Path.Combine(Path.GetTempPath(), $"waller-atomic-write-tests-{Guid.NewGuid():N}");
        try
        {
            var path = Path.Combine(root, "data.bin");
            using var cts = new CancellationTokenSource();
            cts.Cancel();

            await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
                AtomicFileWriter.WriteAsync(
                    path,
                    (_, _) => Task.CompletedTask,
                    cts.Token));

            Assert.False(Directory.Exists(root));
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
    public async Task SolidColorPngWriter_RejectsNullPixels()
    {
        PixelBuffer? pixels = null;

        var error = await Assert.ThrowsAsync<ArgumentNullException>(() =>
            SolidColorPngWriter.WriteAsync("wallpaper.png", pixels!));

        Assert.Equal("pixels", error.ParamName);
    }

    [Fact]
    public void PixelBuffer_RejectsInvalidDimensions()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new PixelBuffer(0, 1, []));
        Assert.Throws<ArgumentOutOfRangeException>(() => new PixelBuffer(1, 0, []));
    }

    [Fact]
    public void PixelBuffer_RejectsNullData()
    {
        byte[]? data = null;

        var error = Assert.Throws<ArgumentNullException>(() => new PixelBuffer(1, 1, data!));

        Assert.Equal("data", error.ParamName);
    }

    [Fact]
    public void PixelBuffer_RejectsInvalidDataLength()
    {
        var error = Assert.Throws<ArgumentException>(() => new PixelBuffer(2, 2, new byte[3]));

        Assert.Equal("data", error.ParamName);
    }

    [Fact]
    public void PixelBuffer_CopiesInputData()
    {
        var data = new byte[] { 1, 2, 3 };
        var buffer = new PixelBuffer(1, 1, data);

        data[0] = 9;

        Assert.Equal(new RgbColor(1, 2, 3), buffer.GetPixel(0, 0));
    }

    [Theory]
    [InlineData(-1, 0, "x")]
    [InlineData(1, 0, "x")]
    [InlineData(0, -1, "y")]
    [InlineData(0, 1, "y")]
    public void PixelBuffer_GetPixelRejectsOutOfBoundsCoordinates(int x, int y, string parameterName)
    {
        var buffer = new PixelBuffer(1, 1, new byte[3]);

        var error = Assert.Throws<ArgumentOutOfRangeException>(() => buffer.GetPixel(x, y));

        Assert.Equal(parameterName, error.ParamName);
    }

    [Theory]
    [InlineData(-1, 0, "x")]
    [InlineData(1, 0, "x")]
    [InlineData(0, -1, "y")]
    [InlineData(0, 1, "y")]
    public void PixelBuffer_SetPixelRejectsOutOfBoundsCoordinates(int x, int y, string parameterName)
    {
        var buffer = new PixelBuffer(1, 1, new byte[3]);

        var error = Assert.Throws<ArgumentOutOfRangeException>(() =>
            buffer.SetPixel(x, y, RgbColor.Black));

        Assert.Equal(parameterName, error.ParamName);
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
    public void DesktopWallpaperInterop_RejectsUnknownWindowsPosition()
    {
        var error = Assert.Throws<ArgumentOutOfRangeException>(() =>
            DesktopWallpaperInterop.PositionToPlacement((DesktopWallpaperPosition)999));

        Assert.Equal("position", error.ParamName);
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
    public void StaThreadRunner_RunsActionOnStaThreadFromMtaCaller()
    {
        ApartmentState? callerApartment = null;
        ApartmentState? actionApartment = null;

        var thread = new Thread(() =>
        {
            callerApartment = Thread.CurrentThread.GetApartmentState();
            StaThreadRunner.Run(() =>
            {
                actionApartment = Thread.CurrentThread.GetApartmentState();
            });
        });

        thread.SetApartmentState(ApartmentState.MTA);
        thread.Start();
        thread.Join();

        Assert.Equal(ApartmentState.MTA, callerApartment);
        Assert.Equal(ApartmentState.STA, actionApartment);
    }

    [Fact]
    public void StaThreadRunner_PropagatesExceptionFromStaThread()
    {
        Exception? observed = null;

        var thread = new Thread(() =>
        {
            observed = Record.Exception(() =>
                StaThreadRunner.Run(() => throw new InvalidOperationException("COM failed.")));
        });

        thread.SetApartmentState(ApartmentState.MTA);
        thread.Start();
        thread.Join();

        var error = Assert.IsType<InvalidOperationException>(observed);
        Assert.Equal("COM failed.", error.Message);
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
    public async Task WallpaperSourceFiles_NormalizesImagePathsBeforeFileChecks()
    {
        var path = Path.Combine(Path.GetTempPath(), $"waller-source-file-{Guid.NewGuid():N}.png");
        try
        {
            var source = new WallpaperSource(WallpaperSourceKind.Image, $" {path} ");
            Assert.True(WallpaperSourceFiles.IsMissingImageFile(source));
            Assert.False(WallpaperSourceFiles.HasExistingImageFile(source));
            Assert.Equal(Path.GetFileName(path), WallpaperSourceFiles.ImageFileName(source));

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
    public void WallpaperSourceFiles_IgnoresInvalidImagePaths()
    {
        var source = new WallpaperSource(WallpaperSourceKind.Image, "relative\\wallpaper.png");

        Assert.False(WallpaperSourceFiles.IsMissingImageFile(source));
        Assert.False(WallpaperSourceFiles.HasExistingImageFile(source));
        Assert.Null(WallpaperSourceFiles.ImageFileName(source));
    }

    [Fact]
    public void WallpaperSourceFiles_IgnoresNonImageSources()
    {
        var source = WallpaperSource.FromSolidColor("#112233");

        Assert.False(WallpaperSourceFiles.IsMissingImageFile(source));
        Assert.False(WallpaperSourceFiles.HasExistingImageFile(source));
        Assert.Null(WallpaperSourceFiles.ImageFileName(source));
    }

    [Theory]
    [InlineData("missing")]
    [InlineData("existing")]
    [InlineData("file-name")]
    public void WallpaperSourceFiles_RejectsNullSource(string operation)
    {
        WallpaperSource? source = null;

        var error = Assert.Throws<ArgumentNullException>(() =>
        {
            _ = operation switch
            {
                "missing" => WallpaperSourceFiles.IsMissingImageFile(source!),
                "existing" => WallpaperSourceFiles.HasExistingImageFile(source!),
                "file-name" => WallpaperSourceFiles.ImageFileName(source!) is not null,
                _ => throw new InvalidOperationException($"Unknown operation: {operation}"),
            };
        });

        Assert.Equal("source", error.ParamName);
    }

    [Theory]
    [InlineData("monitor")]
    [InlineData("path")]
    [InlineData("width")]
    [InlineData("height")]
    public void RenderedWallpaper_RejectsInvalidInputs(string parameterName)
    {
        var monitor = new MonitorIdentity("DISPLAY-1", "Monitor 1", 1, 1920, 1080, 0, 0);
        var maybeMonitor = parameterName == "monitor" ? null : monitor;
        var path = parameterName == "path" ? " " : @"C:\Wallpapers\rendered.png";
        var width = parameterName == "width" ? 0 : 1920;
        var height = parameterName == "height" ? 0 : 1080;

        var error = Assert.ThrowsAny<ArgumentException>(() =>
            new RenderedWallpaper(maybeMonitor!, path, width, height, DateTimeOffset.UtcNow));

        Assert.Equal(parameterName, error.ParamName);
    }

    [Fact]
    public void RenderedWallpaper_RejectsRelativePath()
    {
        var monitor = new MonitorIdentity("DISPLAY-1", "Monitor 1", 1, 1920, 1080, 0, 0);

        var error = Assert.Throws<ArgumentException>(() =>
            new RenderedWallpaper(monitor, "rendered.png", 1920, 1080, DateTimeOffset.UtcNow));

        Assert.Equal("path", error.ParamName);
        Assert.Contains("Rendered wallpaper path must be absolute.", error.Message);
    }

    [Theory]
    [InlineData("monitor")]
    [InlineData("assignment")]
    public void RenderRequest_RejectsNullInputs(string parameterName)
    {
        var monitor = CreateMonitor("DISPLAY-1", 32, 18, WallpaperSource.Empty);
        var assignment = new PresetAssignment(
            monitor.Identity,
            WallpaperSource.Empty,
            WallpaperPlacement.Default);
        MonitorSnapshot? maybeMonitor = parameterName == "monitor" ? null : monitor;
        PresetAssignment? maybeAssignment = parameterName == "assignment" ? null : assignment;

        var error = Assert.Throws<ArgumentNullException>(() =>
            new RenderRequest(maybeMonitor!, maybeAssignment!));

        Assert.Equal(parameterName, error.ParamName);
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
        Assert.Null(result.ErrorMessage);
    }

    [Fact]
    public void DesktopWallpaperApplier_RejectsNullWriter()
    {
        IDesktopWallpaperWriter? writer = null;

        var error = Assert.Throws<ArgumentNullException>(() => new DesktopWallpaperApplier(writer!));

        Assert.Equal("writer", error.ParamName);
    }

    [Fact]
    public async Task DesktopWallpaperApplier_RejectsNullWallpaper()
    {
        var applier = new DesktopWallpaperApplier(new RecordingDesktopWallpaperWriter());
        RenderedWallpaper? wallpaper = null;

        var error = await Assert.ThrowsAsync<ArgumentNullException>(() => applier.ApplyAsync(wallpaper!));

        Assert.Equal("wallpaper", error.ParamName);
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
            Assert.Null(result.ErrorMessage);
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
    public async Task DesktopWallpaperApplier_PropagatesWriterCancellation()
    {
        var path = Path.Combine(Path.GetTempPath(), $"waller-applier-{Guid.NewGuid():N}.png");
        try
        {
            await File.WriteAllBytesAsync(path, [1, 2, 3]);
            var applier = new DesktopWallpaperApplier(new ThrowingDesktopWallpaperWriter(
                new OperationCanceledException()));
            var monitor = new MonitorIdentity("DISPLAY-1", "Monitor 1", 1, 1920, 1080, 0, 0);
            var wallpaper = new RenderedWallpaper(monitor, path, 1920, 1080, DateTimeOffset.UtcNow);

            await Assert.ThrowsAsync<OperationCanceledException>(() => applier.ApplyAsync(wallpaper));
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
    public void ApplyResult_SuccessPreservesMonitor()
    {
        var monitor = new MonitorIdentity("DISPLAY-1", "Monitor 1", 1, 1920, 1080, 0, 0);

        var result = ApplyResult.Success(monitor);

        Assert.Same(monitor, result.Monitor);
    }

    [Fact]
    public void ApplyResult_RejectsNullSuccessMonitor()
    {
        MonitorIdentity? monitor = null;

        var error = Assert.Throws<ArgumentNullException>(() => ApplyResult.Success(monitor!));

        Assert.Equal("monitor", error.ParamName);
    }

    [Fact]
    public void ApplyResult_RejectsNullFailureMonitor()
    {
        MonitorIdentity? monitor = null;

        var error = Assert.Throws<ArgumentNullException>(() => ApplyResult.Failure(
            monitor!,
            ApplyErrorCodes.WallpaperApplyFailed));

        Assert.Equal("monitor", error.ParamName);
    }

    [Fact]
    public void ApplyResult_DoesNotExposePublicConstructors()
    {
        var constructors = typeof(ApplyResult).GetConstructors();

        Assert.Empty(constructors);
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

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("driver exploded")]
    public void ApplyErrorCodes_NormalizeFallsBackForUnknownErrorCode(string? errorCode)
    {
        Assert.Equal(
            ApplyErrorCodes.WallpaperApplyFailed,
            ApplyErrorCodes.Normalize(errorCode));
    }

    [Fact]
    public void ApplyErrorCodes_NormalizePreservesKnownErrorCode()
    {
        Assert.Equal(
            ApplyErrorCodes.RenderedWallpaperMissing,
            ApplyErrorCodes.Normalize(ApplyErrorCodes.RenderedWallpaperMissing));
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

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void DesktopMonitorDisplayName_RejectsBlankMonitorId(string? monitorId)
    {
        var error = Assert.ThrowsAny<ArgumentException>(() =>
            DesktopMonitorDisplayName.ShortenDeviceName(monitorId!));

        Assert.Equal("monitorId", error.ParamName);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void DesktopMonitorDisplayName_RejectsInvalidDisplayIndex(int displayIndex)
    {
        var error = Assert.Throws<ArgumentOutOfRangeException>(() =>
            DesktopMonitorDisplayName.Create(displayIndex, "DISPLAY-1"));

        Assert.Equal("displayIndex", error.ParamName);
    }

    [Fact]
    public void DesktopMonitorDisplayName_TruncatesLongDeviceIds()
    {
        var deviceId = new string('A', 60);

        var displayName = DesktopMonitorDisplayName.Create(2, deviceId);

        Assert.Equal($"Monitor 2 - {new string('A', 45)}...", displayName);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void DesktopWallpaperSnapshot_RejectsBlankMonitorId(string? monitorId)
    {
        var error = Assert.ThrowsAny<ArgumentException>(() => new DesktopWallpaperSnapshot(
            monitorId!,
            new MonitorBounds(0, 0, 1920, 1080),
            null));

        Assert.Equal("MonitorId", error.ParamName);
    }

    [Fact]
    public void DesktopWallpaperSnapshot_RejectsNullBounds()
    {
        MonitorBounds? bounds = null;

        var error = Assert.Throws<ArgumentNullException>(() => new DesktopWallpaperSnapshot(
            "DISPLAY-1",
            bounds!,
            null));

        Assert.Equal("Bounds", error.ParamName);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void DesktopWallpaperSnapshot_WithExpressionRejectsBlankMonitorId(string? monitorId)
    {
        var snapshot = new DesktopWallpaperSnapshot(
            "DISPLAY-1",
            new MonitorBounds(0, 0, 1920, 1080),
            null);

        var error = Assert.ThrowsAny<ArgumentException>(() => snapshot with { MonitorId = monitorId! });

        Assert.Equal("value", error.ParamName);
    }

    [Fact]
    public void DesktopWallpaperSnapshot_WithExpressionRejectsNullBounds()
    {
        var snapshot = new DesktopWallpaperSnapshot(
            "DISPLAY-1",
            new MonitorBounds(0, 0, 1920, 1080),
            null);
        MonitorBounds? bounds = null;

        var error = Assert.Throws<ArgumentNullException>(() => snapshot with { Bounds = bounds! });

        Assert.Equal("value", error.ParamName);
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
    public void BasicPngWallpaperRenderer_RejectsNullStore()
    {
        RenderedWallpaperStore? store = null;

        var error = Assert.Throws<ArgumentNullException>(() => new BasicPngWallpaperRenderer(store!));

        Assert.Equal("store", error.ParamName);
    }

    [Fact]
    public async Task BasicPngWallpaperRenderer_RejectsNullRenderRequest()
    {
        var root = Path.Combine(Path.GetTempPath(), $"waller-render-tests-{Guid.NewGuid():N}");
        try
        {
            var renderer = new BasicPngWallpaperRenderer(new RenderedWallpaperStore(root));
            RenderRequest? request = null;

            var error = await Assert.ThrowsAsync<ArgumentNullException>(() => renderer.RenderAsync(request!));

            Assert.Equal("request", error.ParamName);
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

    [Theory]
    [InlineData("DrawWidth")]
    [InlineData("DrawHeight")]
    public void ImagePlacementPlan_RejectsInvalidDirectDrawDimensions(string parameterName)
    {
        var drawWidth = parameterName == "DrawWidth" ? 0 : 2;
        var drawHeight = parameterName == "DrawHeight" ? 0 : 1;

        var error = Assert.Throws<ArgumentOutOfRangeException>(() =>
            new ImagePlacementPlan(false, 0, 0, drawWidth, drawHeight));

        Assert.Equal(parameterName, error.ParamName);
    }

    [Theory]
    [InlineData("DrawWidth")]
    [InlineData("DrawHeight")]
    public void ImagePlacementPlan_WithExpressionRejectsInvalidDrawDimensions(string propertyName)
    {
        var plan = new ImagePlacementPlan(false, 0, 0, 2, 1);

        var error = Assert.Throws<ArgumentOutOfRangeException>(() => propertyName == "DrawWidth"
            ? plan with { DrawWidth = 0 }
            : plan with { DrawHeight = 0 });

        Assert.Equal("value", error.ParamName);
    }

    [Fact]
    public void ImagePlacementPlan_RejectsNullPlacement()
    {
        WallpaperPlacement? placement = null;

        var error = Assert.Throws<ArgumentNullException>(() =>
            ImagePlacementPlan.Create(
                sourceWidth: 1,
                sourceHeight: 1,
                targetWidth: 2,
                targetHeight: 2,
                placement!));

        Assert.Equal("placement", error.ParamName);
    }

    [Theory]
    [InlineData("source")]
    [InlineData("placement")]
    public void ImagePlacementRenderer_RejectsNullInputs(string parameterName)
    {
        var source = new PixelBuffer(1, 1, new byte[3]);
        PixelBuffer? maybeSource = parameterName == "source" ? null : source;
        WallpaperPlacement? maybePlacement = parameterName == "placement" ? null : WallpaperPlacement.Default;

        var error = Assert.Throws<ArgumentNullException>(() =>
            ImagePlacementRenderer.Render(maybeSource!, 2, 2, maybePlacement!));

        Assert.Equal(parameterName, error.ParamName);
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

    [Theory]
    [InlineData("renderer")]
    [InlineData("applier")]
    public void WallpaperApplyService_RejectsNullDependencies(string parameterName)
    {
        var renderer = new PassthroughWallpaperRenderer();
        var applier = new RecordingWallpaperApplier(succeed: true);
        IWallpaperRenderer? maybeRenderer = parameterName == "renderer" ? null : renderer;
        IWallpaperApplier? maybeApplier = parameterName == "applier" ? null : applier;

        var error = Assert.Throws<ArgumentNullException>(() =>
            new WallpaperApplyService(maybeRenderer!, maybeApplier!));

        Assert.Equal(parameterName, error.ParamName);
    }

    [Theory]
    [InlineData("monitor")]
    [InlineData("monitor-ready")]
    [InlineData("all")]
    [InlineData("all-ready")]
    [InlineData("matching")]
    public async Task WallpaperApplyService_RejectsNullSession(string applyMode)
    {
        var service = new WallpaperApplyService(
            new PassthroughWallpaperRenderer(),
            new RecordingWallpaperApplier(succeed: true));
        ActiveSession? session = null;

        var error = await Assert.ThrowsAsync<ArgumentNullException>(() => applyMode switch
        {
            "monitor" => service.ApplyMonitorAsync(session!, "DISPLAY-1"),
            "monitor-ready" => service.ApplyMonitorReadySourceAsync(session!, "DISPLAY-1"),
            "all" => service.ApplyAllAsync(session!),
            "all-ready" => service.ApplyAllReadySourcesAsync(session!),
            "matching" => service.ApplyMatchingAsync(session!, _ => true),
            _ => throw new InvalidOperationException($"Unknown apply mode: {applyMode}"),
        });

        Assert.Equal("session", error.ParamName);
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
    public async Task WallpaperApplyService_LeavesSuccessfulMonitorsAppliedWhenLaterMonitorFails()
    {
        var root = Path.Combine(Path.GetTempPath(), $"waller-apply-tests-{Guid.NewGuid():N}");
        try
        {
            var renderer = new BasicPngWallpaperRenderer(new RenderedWallpaperStore(root));
            var applier = new FailingMonitorWallpaperApplier("DISPLAY-2");
            var service = new WallpaperApplyService(renderer, applier);
            var first = CreateMonitor("DISPLAY-1", 16, 16, WallpaperSource.FromSolidColor("#112233"));
            var second = CreateMonitor("DISPLAY-2", 16, 16, WallpaperSource.FromSolidColor("#445566"));
            var session = ActiveSession.FromMonitors([first, second]);

            var result = await service.ApplyAllAsync(session);

            Assert.Equal(1, result.Succeeded);
            Assert.Equal(1, result.Failed);
            Assert.Equal(MonitorApplyStatus.Applied, result.Session.Monitors[0].ApplyStatus);
            Assert.Null(result.Session.Monitors[0].ApplyError);
            Assert.Equal(MonitorApplyStatus.Error, result.Session.Monitors[1].ApplyStatus);
            Assert.Equal(ApplyErrorCodes.WallpaperApplyFailed, result.Session.Monitors[1].ApplyError);
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
    public void WallpaperRenderException_NormalizesUnknownErrorCode()
    {
        var error = new WallpaperRenderException("driver exploded", "render failed");

        Assert.Equal(ApplyErrorCodes.WallpaperApplyFailed, error.ErrorCode);
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
    public void ApplySessionResult_NoneCreatesNoOutcomeResult()
    {
        var session = ActiveSession.FromMonitors([]);

        var result = ApplySessionResult.None(session);

        Assert.Equal(0, result.Succeeded);
        Assert.Equal(0, result.Failed);
        Assert.Equal(0, result.Skipped);
        Assert.False(result.HasAnyOutcome);
        Assert.Same(session, result.Session);
    }

    [Fact]
    public void ApplySessionResult_SkippedOnlyCreatesSkippedOutcome()
    {
        var session = ActiveSession.FromMonitors([]);

        var result = ApplySessionResult.SkippedOnly(session, skipped: 3);

        Assert.Equal(0, result.Succeeded);
        Assert.Equal(0, result.Failed);
        Assert.Equal(3, result.Skipped);
        Assert.True(result.HasAnyOutcome);
        Assert.False(result.HasAppliedOutcome);
        Assert.Same(session, result.Session);
    }

    [Theory]
    [InlineData(-1, 0, 0, "Succeeded")]
    [InlineData(0, -1, 0, "Failed")]
    [InlineData(0, 0, -1, "Skipped")]
    public void ApplySessionResult_RejectsNegativeCounts(
        int succeeded,
        int failed,
        int skipped,
        string parameterName)
    {
        var session = ActiveSession.FromMonitors([]);

        var error = Assert.Throws<ArgumentOutOfRangeException>(() =>
            new ApplySessionResult(session, succeeded, failed, skipped));

        Assert.Equal(parameterName, error.ParamName);
    }

    [Fact]
    public void ApplySessionResult_RejectsNullSession()
    {
        ActiveSession? session = null;

        var error = Assert.Throws<ArgumentNullException>(() =>
            new ApplySessionResult(session!, Succeeded: 0, Failed: 0));

        Assert.Equal("session", error.ParamName);
    }

    [Fact]
    public void ApplySessionResult_WithSkippedRejectsNegativeSkippedCount()
    {
        var session = ActiveSession.FromMonitors([]);
        var result = ApplySessionResult.None(session);

        var error = Assert.Throws<ArgumentOutOfRangeException>(() => result.WithSkipped(-1));

        Assert.Equal("Skipped", error.ParamName);
    }

    [Theory]
    [InlineData(-1, 0, "Completed")]
    [InlineData(0, -1, "Total")]
    [InlineData(2, 1, "Completed")]
    public void ApplyProgress_RejectsInvalidCounts(
        int completed,
        int total,
        string parameterName)
    {
        var error = Assert.Throws<ArgumentOutOfRangeException>(() =>
            new ApplyProgress(completed, total, "DISPLAY-1", MonitorApplyStatus.Applying));

        Assert.Equal(parameterName, error.ParamName);
    }

    [Fact]
    public void ApplyProgress_RejectsNullMonitorName()
    {
        string? monitorName = null;

        var error = Assert.Throws<ArgumentNullException>(() =>
            new ApplyProgress(0, 1, monitorName!, MonitorApplyStatus.Applying));

        Assert.Equal("MonitorName", error.ParamName);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void ApplyProgress_RejectsBlankMonitorName(string monitorName)
    {
        var error = Assert.Throws<ArgumentException>(() =>
            new ApplyProgress(0, 1, monitorName, MonitorApplyStatus.Applying));

        Assert.Equal("MonitorName", error.ParamName);
    }

    [Fact]
    public void ApplyProgress_TrimsMonitorName()
    {
        var progress = new ApplyProgress(0, 1, "  DISPLAY-1  ", MonitorApplyStatus.Applying);

        Assert.Equal("DISPLAY-1", progress.MonitorName);
    }

    [Fact]
    public void ApplyProgress_RejectsInvalidStatus()
    {
        var error = Assert.Throws<ArgumentOutOfRangeException>(() =>
            new ApplyProgress(0, 1, "DISPLAY-1", (MonitorApplyStatus)999));

        Assert.Equal("value", error.ParamName);
    }

    [Fact]
    public void ApplyProgress_WithExpressionRejectsInvalidStatus()
    {
        var progress = new ApplyProgress(0, 1, "DISPLAY-1", MonitorApplyStatus.Applying);

        var error = Assert.Throws<ArgumentOutOfRangeException>(() =>
            progress with { Status = (MonitorApplyStatus)999 });

        Assert.Equal("value", error.ParamName);
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
    public void ApplyCanceledException_RejectsNullResult()
    {
        ApplySessionResult? result = null;

        var error = Assert.Throws<ArgumentNullException>(() =>
            new ApplyCanceledException(result!));

        Assert.Equal("result", error.ParamName);
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

    [Theory]
    [InlineData("success")]
    [InlineData("failure")]
    public void ApplyRunTracker_RejectsRecordingPastTotal(string outcome)
    {
        var tracker = new ApplyRunTracker(total: 1, progress: null);
        tracker.RecordSuccess();

        var error = Assert.Throws<InvalidOperationException>(() =>
        {
            if (outcome == "success")
            {
                tracker.RecordSuccess();
                return;
            }

            tracker.RecordFailure();
        });

        Assert.Equal(
            "Apply tracker cannot record more completed steps than its total.",
            error.Message);
    }

    [Theory]
    [InlineData("starting")]
    [InlineData("completed")]
    public void ApplyRunTracker_RejectsNullProgressMonitor(string action)
    {
        var tracker = new ApplyRunTracker(total: 1, progress: null);
        MonitorSession? monitor = null;

        var error = Assert.Throws<ArgumentNullException>(() =>
        {
            if (action == "starting")
            {
                tracker.ReportStarting(monitor!);
                return;
            }

            tracker.ReportCompleted(monitor!);
        });

        Assert.Equal("monitor", error.ParamName);
    }

    [Fact]
    public void ApplyRunTracker_RejectsNullStepResult()
    {
        var tracker = new ApplyRunTracker(total: 1, progress: null);
        MonitorApplyStepResult? result = null;

        var error = Assert.Throws<ArgumentNullException>(() => tracker.Record(result!));

        Assert.Equal("result", error.ParamName);
    }

    [Theory]
    [InlineData("result-session", "session")]
    [InlineData("result-monitors", "monitors")]
    [InlineData("canceled-session", "session")]
    [InlineData("canceled-monitors", "monitors")]
    public void ApplyRunTracker_RejectsNullResultProjectionInputs(string action, string parameterName)
    {
        var monitor = CreateMonitor("DISPLAY-1", 16, 16, WallpaperSource.FromSolidColor("#112233"));
        var session = ActiveSession.FromMonitors([monitor]);
        var tracker = new ApplyRunTracker(total: 1, progress: null);
        ActiveSession? maybeSession = action.EndsWith("session", StringComparison.Ordinal) ? null : session;
        IReadOnlyList<MonitorSession>? maybeMonitors = action.EndsWith("monitors", StringComparison.Ordinal)
            ? null
            : session.Monitors;

        var error = Assert.Throws<ArgumentNullException>(() =>
        {
            if (action.StartsWith("result", StringComparison.Ordinal))
            {
                tracker.ToResult(maybeSession!, maybeMonitors!);
                return;
            }

            tracker.ToCanceledException(maybeSession!, maybeMonitors!);
        });

        Assert.Equal(parameterName, error.ParamName);
    }

    [Theory]
    [InlineData("result")]
    [InlineData("canceled")]
    public void ApplyRunTracker_RejectsNullResultProjectionMonitorItems(string action)
    {
        var monitor = CreateMonitor("DISPLAY-1", 16, 16, WallpaperSource.FromSolidColor("#112233"));
        var session = ActiveSession.FromMonitors([monitor]);
        var tracker = new ApplyRunTracker(total: 1, progress: null);
        IReadOnlyList<MonitorSession> monitors = [session.Monitors[0], null!];

        var error = Assert.Throws<ArgumentException>(() =>
        {
            if (action == "result")
            {
                tracker.ToResult(session, monitors);
                return;
            }

            tracker.ToCanceledException(session, monitors);
        });

        Assert.Equal("monitors", error.ParamName);
        Assert.Equal(
            "Apply result monitor list cannot include null items. (Parameter 'monitors')",
            error.Message);
    }

    [Fact]
    public void MonitorApplyStepResult_RejectsNullMonitor()
    {
        MonitorSession? monitor = null;

        var successError = Assert.Throws<ArgumentNullException>(() =>
            MonitorApplyStepResult.Success(monitor!));
        var failureError = Assert.Throws<ArgumentNullException>(() =>
            MonitorApplyStepResult.Failure(monitor!, "failed"));

        Assert.Equal("monitor", successError.ParamName);
        Assert.Equal("monitor", failureError.ParamName);
    }

    [Fact]
    public void MonitorApplyStepResult_DoesNotExposePublicConstructors()
    {
        var constructors = typeof(MonitorApplyStepResult).GetConstructors();

        Assert.Empty(constructors);
    }

    [Fact]
    public void MonitorApplyStepResult_SuccessAppliesAssignment()
    {
        var snapshot = CreateMonitor("DISPLAY-1", 16, 16, WallpaperSource.FromSolidColor("#112233"));
        var monitor = ActiveSession.FromMonitors([snapshot]).Monitors[0];

        var result = MonitorApplyStepResult.Success(monitor);

        Assert.True(result.Succeeded);
        Assert.Equal(MonitorApplyStatus.Applied, result.Monitor.ApplyStatus);
        Assert.Null(result.Monitor.ApplyError);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("driver exploded")]
    public void MonitorApplyStepResult_FailureFallsBackForUnknownErrorCode(string? errorCode)
    {
        var snapshot = CreateMonitor("DISPLAY-1", 16, 16, WallpaperSource.FromSolidColor("#112233"));
        var monitor = ActiveSession.FromMonitors([snapshot]).Monitors[0];

        var result = MonitorApplyStepResult.Failure(monitor, errorCode);

        Assert.False(result.Succeeded);
        Assert.Equal(MonitorApplyStatus.Error, result.Monitor.ApplyStatus);
        Assert.Equal(ApplyErrorCodes.WallpaperApplyFailed, result.Monitor.ApplyError);
    }

    [Fact]
    public void MonitorApplyStepResult_FailureMarksApplyError()
    {
        var snapshot = CreateMonitor("DISPLAY-1", 16, 16, WallpaperSource.FromSolidColor("#112233"));
        var monitor = ActiveSession.FromMonitors([snapshot]).Monitors[0];

        var result = MonitorApplyStepResult.Failure(monitor, ApplyErrorCodes.WallpaperApplyFailed);

        Assert.False(result.Succeeded);
        Assert.Equal(MonitorApplyStatus.Error, result.Monitor.ApplyStatus);
        Assert.Equal(ApplyErrorCodes.WallpaperApplyFailed, result.Monitor.ApplyError);
    }

    [Fact]
    public void ApplyRunTracker_RejectsNegativeTotal()
    {
        var error = Assert.Throws<ArgumentOutOfRangeException>(() =>
            new ApplyRunTracker(total: -1, progress: null));

        Assert.Equal("total", error.ParamName);
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
            var progressEvents = new List<ApplyProgress>();

            var result = await service.ApplyAllReadySourcesAsync(session, progress => progressEvents.Add(progress));

            Assert.Equal(0, result.Succeeded);
            Assert.Equal(0, result.Failed);
            Assert.Equal(2, result.Skipped);
            Assert.Empty(progressEvents);
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
    public async Task WallpaperApplyService_ApplyAllReadySourcesWithNoMonitorsDoesNotRender()
    {
        var root = Path.Combine(Path.GetTempPath(), $"waller-apply-tests-{Guid.NewGuid():N}");
        try
        {
            var applier = new RecordingWallpaperApplier(succeed: true);
            var service = CreateApplyService(root, applier);
            var session = ActiveSession.FromMonitors([]);

            var result = await service.ApplyAllReadySourcesAsync(session);

            Assert.Equal(0, result.Succeeded);
            Assert.Equal(0, result.Failed);
            Assert.Equal(0, result.Skipped);
            Assert.Empty(result.Session.Monitors);
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

    private sealed class FailingMonitorWallpaperApplier(string failedMonitorKey) : IWallpaperApplier
    {
        public Task<ApplyResult> ApplyAsync(
            RenderedWallpaper wallpaper,
            CancellationToken cancellationToken = default)
        {
            var result = string.Equals(
                wallpaper.Monitor.MonitorKey,
                failedMonitorKey,
                StringComparison.OrdinalIgnoreCase)
                ? ApplyResult.Failure(wallpaper.Monitor, ApplyErrorCodes.WallpaperApplyFailed)
                : ApplyResult.Success(wallpaper.Monitor);

            return Task.FromResult(result);
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

    private sealed class ThrowingMonitorDetector(Exception error) : IMonitorDetector
    {
        public Task<IReadOnlyList<MonitorSnapshot>> DetectAsync(CancellationToken cancellationToken = default)
        {
            throw error;
        }
    }

}
