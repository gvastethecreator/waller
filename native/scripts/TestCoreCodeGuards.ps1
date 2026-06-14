param(
    [string]$CorePath = ".\Waller.Native.Core"
)

$ErrorActionPreference = "Stop"

$nativeRoot = Split-Path -Parent $PSScriptRoot
$resolvedPath = if ([System.IO.Path]::IsPathRooted($CorePath)) {
    $CorePath
}
else {
    Join-Path $nativeRoot $CorePath
}

if (-not (Test-Path -LiteralPath $resolvedPath)) {
    throw "Core path not found: $resolvedPath"
}

function Test-CoreTextContracts {
    param([array]$Contracts)

    $violations = @()
    foreach ($contract in $Contracts) {
        $contractPath = Join-Path $resolvedPath $contract.Path
        if (-not (Test-Path -LiteralPath $contractPath)) {
            $violations += "$($contract.Path): file missing"
            continue
        }

        $contractText = Get-Content -LiteralPath $contractPath -Raw
        if ($contract.PositionalPattern -and $contractText -match $contract.PositionalPattern) {
            $violations += "$($contract.Path): positional record"
        }

        foreach ($required in $contract.Required) {
            if (-not $contractText.Contains($required)) {
                $violations += "$($contract.Path): $required"
            }
        }
    }

    return $violations
}

$modelContracts = @(
    @{
        Path = "Models\MonitorDisplayName.cs"
        PositionalPattern = $null
        Required = @(
            "internal static class MonitorDisplayName",
            "public static string Normalize(string displayName, string parameterName)",
            "displayName.Trim()",
            "Monitor display name is required."
        )
    },
    @{
        Path = "Models\MonitorIdentity.cs"
        PositionalPattern = 'public\s+sealed\s+record\s+MonitorIdentity\s*\('
        Required = @(
            "private string monitorKey = string.Empty",
            "init => monitorKey = value ?? string.Empty",
            "!string.IsNullOrWhiteSpace(MonitorKey)",
            "Width > 0",
            "Height > 0"
        )
    },
    @{
        Path = "Models\MonitorKeys.cs"
        PositionalPattern = $null
        Required = @(
            "public static string Require(string monitorKey, string parameterName)",
            "public static bool Contains(IReadOnlySet<string> monitorKeys, string monitorKey)",
            "Monitor key is required.",
            "Require(monitorKey, nameof(monitorKey))",
            "Require(monitorKey, nameof(monitorKeys))"
        )
    },
    @{
        Path = "Models\WallpaperSource.cs"
        PositionalPattern = 'public\s+sealed\s+record\s+WallpaperSource\s*\('
        Required = @(
            "private WallpaperSourceKind kind",
            "DefinedEnumValue.Require(",
            "DefinedEnumValue.IsDefined(source.Kind)",
            "TryNormalize(WallpaperSource? source)"
        )
    },
    @{
        Path = "Models\DefinedEnumValue.cs"
        PositionalPattern = $null
        Required = @(
            "public static class DefinedEnumValue",
            "public static bool IsDefined<T>(T value)",
            "public static T Require<T>(T value, string parameterName, string message)",
            "where T : struct, Enum",
            "throw new ArgumentOutOfRangeException(parameterName, value, message)"
        )
    },
    @{
        Path = "Models\WallpaperPlacement.cs"
        PositionalPattern = $null
        Required = @(
            "private WallpaperFitMode fitMode",
            "private WallpaperAnchor anchor",
            "DefinedEnumValue.Require(",
            "Wallpaper fit mode is invalid.",
            "Wallpaper anchor is invalid."
        )
    },
    @{
        Path = "Models\MonitorSnapshot.cs"
        PositionalPattern = $null
        Required = @(
            "ArgumentNullException.ThrowIfNull(identity)",
            "MonitorDisplayName.Normalize(displayName, nameof(displayName))",
            "ArgumentNullException.ThrowIfNull(currentSource)"
        )
    },
    @{
        Path = "Models\MonitorSession.cs"
        PositionalPattern = $null
        Required = @(
            "private MonitorApplyStatus applyStatus",
            "DefinedEnumValue.Require(",
            "Monitor apply status is invalid"
        )
    },
    @{
        Path = "Models\ApplyErrorCodes.cs"
        PositionalPattern = $null
        Required = @(
            "public static string Normalize(string? errorCode)",
            "IsKnown(errorCode)",
            "? errorCode!",
            ": WallpaperApplyFailed"
        )
    },
    @{
        Path = "Models\ColorHexValue.cs"
        PositionalPattern = $null
        Required = @(
            "string.IsNullOrWhiteSpace(colorHex)",
            'throw new ArgumentException("Color must be #RRGGBB.", nameof(colorHex));',
            "HexColorPattern.IsMatch(value)"
        )
    },
    @{
        Path = "Models\WallpaperSourcePath.cs"
        PositionalPattern = $null
        Required = @(
            "TryNormalizeImagePath(imagePath, out var normalized)",
            "File.Exists(normalized)",
            "Path.GetFileName(normalized)"
        )
    },
    @{
        Path = "Models\ActiveSession.cs"
        PositionalPattern = $null
        Required = @(
            "RequiredList.Copy(Monitors, nameof(Monitors)",
            "Active Session monitor list cannot include null items.",
            "Active Session missing assignment list cannot include null items.",
            "Active Session monitor snapshot list cannot include null items.",
            "RequiredList.ValidateItems"
        )
    },
    @{
        Path = "Models\Preset.cs"
        PositionalPattern = $null
        Required = @(
            "PresetIds.RequireValid(Id, nameof(Id))",
            "PresetIds.RequireValid(value, nameof(value))",
            "PresetNames.Validate(Name, nameof(Name))",
            "PresetNames.Validate(value, nameof(value))",
            "RequiredList.Copy(",
            "Preset assignment list cannot include null items."
        )
    },
    @{
        Path = "Models\PresetIds.cs"
        PositionalPattern = $null
        Required = @(
            "public static class PresetIds",
            "public static bool IsValid(Guid id)",
            "public static Guid? NormalizeOptional(Guid? id)",
            "public static Guid RequireValid(Guid id, string parameterName)",
            "id != Guid.Empty",
            "id == Guid.Empty ? null : id",
            "!IsValid(id)",
            "Preset id cannot be empty."
        )
    },
    @{
        Path = "Models\RequiredList.cs"
        PositionalPattern = $null
        Required = @(
            "internal static class RequiredList",
            "public static IReadOnlyList<T> Copy<T>",
            "public static void ValidateItems<T>",
            "ArgumentNullException.ThrowIfNull(value, parameterName)",
            "foreach (var item in value)",
            "throw new ArgumentException(nullItemMessage, parameterName)"
        )
    },
    @{
        Path = "Storage\LocalDataRootDirectory.cs"
        PositionalPattern = $null
        Required = @(
            "internal static class LocalDataRootDirectory",
            "public static string RequireFullyQualified(string rootDirectory)",
            "ArgumentException.ThrowIfNullOrWhiteSpace(rootDirectory)",
            "Path.IsPathFullyQualified(rootDirectory)",
            "Local data root directory must be fully qualified."
        )
    },
    @{
        Path = "Storage\AtomicFileWriter.cs"
        PositionalPattern = $null
        Required = @(
            "internal static string CreateTempPath(string path)",
            "Atomic write path must include a file name.",
            "cancellationToken.ThrowIfCancellationRequested();",
            ".{fileName}.{Guid.NewGuid():N}.tmp",
            "LocalDataFile.DeleteRecoverableIfExists(tempPath)"
        )
    },
    @{
        Path = "Storage\LocalDataFile.cs"
        PositionalPattern = $null
        Required = @(
            "internal static class LocalDataFile",
            "public static void DeleteIfExists(string path)",
            "public static void DeleteRecoverableIfExists(string path)",
            "public static bool TryDeleteIfExists(string path)",
            "ArgumentException.ThrowIfNullOrWhiteSpace(path)",
            "catch (DirectoryNotFoundException)",
            "catch (FileNotFoundException)",
            "LocalDataFileSystemErrors.IsRecoverable(error)",
            "return false;"
        )
    },
    @{
        Path = "Storage\LocalJsonFile.cs"
        PositionalPattern = $null
        Required = @(
            "cancellationToken.ThrowIfCancellationRequested();",
            "File.OpenRead(path)"
        )
    },
    @{
        Path = "Presets\PresetStore.cs"
        PositionalPattern = $null
        Required = @(
            "LocalDataRootDirectory.RequireFullyQualified(rootDirectory)",
            "PresetIds.RequireValid(id, nameof(id))",
            "cancellationToken.ThrowIfCancellationRequested();",
            "return await LoadFromPathAsync(path, cancellationToken);",
            "LocalDataFile.DeleteIfExists(path);"
        )
    },
    @{
        Path = "Presets\PresetFilePolicy.cs"
        PositionalPattern = $null
        Required = @(
            "PresetIds.IsValid(preset.Id)"
        )
    },
    @{
        Path = "Presets\PresetNames.cs"
        PositionalPattern = $null
        Required = @(
            "public static string Validate(string name) => Validate(name, nameof(name));",
            "public static string Validate(string name, string parameterName)",
            "throw new ArgumentNullException(parameterName)",
            'throw new ArgumentException("Preset name is required.", parameterName)',
            "return trimmed;"
        )
    },
    @{
        Path = "Presets\PresetMatcher.cs"
        PositionalPattern = $null
        Required = @(
            "MonitorKeys.Contains(usedAssignmentKeys, assignment.SavedMonitor.MonitorKey)",
            "IReadOnlySet<string> usedAssignmentKeys",
            "new Dictionary<string, PresetAssignment>(MonitorKeys.Comparer)"
        )
    },
    @{
        Path = "Models\PresetAssignments.cs"
        PositionalPattern = $null
        Required = @(
            "DefinedEnumValue.IsDefined(assignment.Placement.FitMode)",
            "DefinedEnumValue.IsDefined(assignment.Placement.Anchor)"
        )
    },
    @{
        Path = "Settings\UserSettingsStore.cs"
        PositionalPattern = $null
        Required = @(
            "LocalDataRootDirectory.RequireFullyQualified(rootDirectory)",
            "cancellationToken.ThrowIfCancellationRequested();",
            "LocalJsonFile.ReadRecoverableAsync("
        )
    },
    @{
        Path = "Rendering\RenderedWallpaperStore.cs"
        PositionalPattern = $null
        Required = @(
            "LocalDataRootDirectory.RequireFullyQualified(rootDirectory)",
            "MonitorKeys.Require(monitorKey, nameof(monitorKey))",
            "LocalDataFile.TryDeleteIfExists(file)",
            "LocalDataFileSystemErrors.IsRecoverable(error)"
        )
    },
    @{
        Path = "Settings\UserSettings.cs"
        PositionalPattern = $null
        Required = @(
            "DefinedEnumValue.Require(theme, nameof(theme),",
            "Theme preference is not supported.",
            "AppLanguages.Normalize(language)",
            "Settings language is not supported.",
            "Language = normalizedLanguage",
            "PresetIds.NormalizeOptional(LastSelectedPresetId)",
            "PresetIds.NormalizeOptional(value)",
            "PresetIds.NormalizeOptional(lastSelectedPresetId)",
            "Math.Max(UserSettingsPolicy.MinWindowWidth, width)",
            "Math.Max(UserSettingsPolicy.MinWindowHeight, height)"
        )
    },
    @{
        Path = "Settings\UserSettingsPolicy.cs"
        PositionalPattern = $null
        Required = @(
            "DefinedEnumValue.IsDefined(settings.Theme)",
            "? settings.Theme",
            ": UserSettings.Default.Theme"
        )
    },
    @{
        Path = "Sessions\ApplyProgress.cs"
        PositionalPattern = $null
        Required = @(
            "private MonitorApplyStatus status",
            "MonitorDisplayName.Normalize(MonitorName, nameof(MonitorName))",
            "DefinedEnumValue.Require(",
            "Monitor apply status is invalid"
        )
    },
    @{
        Path = "Sessions\ActiveSessionEditor.cs"
        PositionalPattern = $null
        Required = @(
            "MonitorKeys.Require(monitorKey, nameof(monitorKey))",
            "MonitorKeys.Require(missingMonitorKey, nameof(missingMonitorKey))",
            "MonitorKeys.Require(targetMonitorKey, nameof(targetMonitorKey))"
        )
    },
    @{
        Path = "Sessions\ApplyPreflight.cs"
        PositionalPattern = $null
        Required = @(
            "MonitorKeys.Require(monitorKey, nameof(monitorKey))",
            "MonitorKeys.Contains(result.SkippedMonitorKeys"
        )
    },
    @{
        Path = "Sessions\ApplyTargetPlan.cs"
        PositionalPattern = $null
        Required = @(
            "MonitorKeys.Require(monitorKey, nameof(monitorKey))",
            "MonitorKeys.Contains(keys"
        )
    },
    @{
        Path = "Sessions\ApplyRunTracker.cs"
        PositionalPattern = $null
        Required = @(
            "private void RecordCompletedStep()",
            "completed >= total",
            "Apply tracker cannot record more completed steps than its total.",
            "RequiredList.Copy(",
            "Apply result monitor list cannot include null items.",
            "RecordCompletedStep();"
        )
    },
    @{
        Path = "Sessions\MonitorApplyStepResult.cs"
        PositionalPattern = $null
        Required = @(
            "private MonitorApplyStepResult(",
            "return new(monitor.WithAppliedAssignment(), Succeeded: true);",
            "public static MonitorApplyStepResult Failure(MonitorSession monitor, string? errorCode)",
            "return new(monitor.WithApplyError(ApplyErrorCodes.Normalize(errorCode)), Succeeded: false);"
        )
    },
    @{
        Path = "Sessions\ApplyPreflightResult.cs"
        PositionalPattern = $null
        Required = @(
            "EnsureDisjoint(ready, skipped)",
            "MonitorKeys.Contains(skippedMonitorKeys, readyMonitorKey)",
            "Apply preflight cannot mark a monitor as both ready and skipped.",
            "nameof(skippedMonitorKeys)"
        )
    },
    @{
        Path = "Rendering\PixelBuffer.cs"
        PositionalPattern = $null
        Required = @(
            "Data = data.ToArray();",
            "x < 0 || x >= Width",
            "y < 0 || y >= Height",
            "Pixel x coordinate is outside the buffer.",
            "Pixel y coordinate is outside the buffer."
        )
    },
    @{
        Path = "Models\RenderedWallpaper.cs"
        PositionalPattern = $null
        Required = @(
            "Path.IsPathRooted(path)",
            "Rendered wallpaper path must be absolute."
        )
    },
    @{
        Path = "Models\ApplyResult.cs"
        PositionalPattern = $null
        Required = @(
            "private ApplyResult(",
            "return new(monitor, Succeeded: true, ErrorCode: null, ErrorMessage: null);",
            "ApplyErrorCodes.Normalize(ErrorCode)"
        )
    },
    @{
        Path = "Rendering\ImagePlacementPlan.cs"
        PositionalPattern = $null
        Required = @(
            "DefinedEnumValue.Require(",
            "Unknown image placement fit mode.",
            "Unknown image placement anchor.",
            "WallpaperFitMode.Cover => GetUniformScale",
            "WallpaperAnchor.Top or WallpaperAnchor.Center or WallpaperAnchor.Bottom",
            "InvalidFitMode(fitMode)",
            "InvalidAnchorX(anchor)",
            "InvalidAnchorY(anchor)"
        )
    },
    @{
        Path = "Rendering\BasicPngWallpaperRenderer.cs"
        PositionalPattern = $null
        Required = @(
            "WallpaperSourceKind.Empty => PixelBuffer.CreateSolid",
            "InvalidSourceKind(request.Assignment.Source.Kind)",
            "Unknown render source kind."
        )
    },
    @{
        Path = "Rendering\WallpaperRenderException.cs"
        PositionalPattern = $null
        Required = @(
            "ApplyErrorCodes.Normalize(errorCode)"
        )
    },
    @{
        Path = "Sessions\ApplyErrorClassifier.cs"
        PositionalPattern = $null
        Required = @(
            "ApplyErrorCodes.Normalize(errorCode)",
            "WallpaperRenderException renderError => FriendlyErrorCode(renderError.ErrorCode)"
        )
    },
    @{
        Path = "Windows\DesktopWallpaperApplier.cs"
        PositionalPattern = $null
        Required = @(
            "catch (Exception error) when (DesktopWallpaperApplyErrors.IsRecoverable(error))",
            "ApplyErrorCodes.WallpaperApplyFailed"
        )
    },
    @{
        Path = "Windows\DesktopWallpaperSnapshot.cs"
        PositionalPattern = $null
        Required = @(
            "MonitorKeys.Require(MonitorId, nameof(MonitorId))",
            "MonitorKeys.Require(value, nameof(value))",
            "ArgumentNullException.ThrowIfNull(Bounds)"
        )
    },
    @{
        Path = "Windows\DesktopMonitorDisplayName.cs"
        PositionalPattern = $null
        Required = @(
            "MonitorKeys.Require(monitorId, nameof(monitorId)).Trim()",
            "displayIndex <= 0",
            "Display index must be positive."
        )
    },
    @{
        Path = "Windows\DesktopWallpaperApplyErrors.cs"
        PositionalPattern = $null
        Required = @(
            "internal static class DesktopWallpaperApplyErrors",
            "ArgumentNullException.ThrowIfNull(error)",
            "error is not OperationCanceledException"
        )
    },
    @{
        Path = "Windows\DesktopWallpaperInterop.cs"
        PositionalPattern = $null
        Required = @(
            "DesktopWallpaperPosition.Fill => WallpaperFitMode.Cover",
            "DesktopWallpaperPosition.Span => WallpaperFitMode.Cover",
            "InvalidDesktopWallpaperPosition(position)",
            "Unknown Windows desktop wallpaper position."
        )
    }
)

$violations = Test-CoreTextContracts $modelContracts
if ($violations.Count -gt 0) {
    Write-Host "Core model contract guards failed:" -ForegroundColor Red
    foreach ($violation in $violations) {
        Write-Host " - $violation" -ForegroundColor Red
    }

    exit 1
}

Write-Host "Core code guards passed."
