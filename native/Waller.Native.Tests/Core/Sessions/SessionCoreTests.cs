using Waller.Native.Core.Models;
using Waller.Native.Core.Presets;
using Waller.Native.Core.Rendering;
using Waller.Native.Core.Sessions;
using Waller.Native.Core.Settings;
using Waller.Native.Core.Storage;
using Waller.Native.Core.Topology;
using Waller.Native.Core.Windows;

namespace Waller.Native.Tests;

public sealed partial class CoreArchitectureTests
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
}
