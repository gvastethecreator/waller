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
    public void PresetAssignments_NormalizeRejectsNullAssignment()
    {
        PresetAssignment? assignment = null;

        var error = Assert.Throws<ArgumentNullException>(() => PresetAssignments.Normalize(assignment!));

        Assert.Equal("assignment", error.ParamName);
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
}
