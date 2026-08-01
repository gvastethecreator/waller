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
}
