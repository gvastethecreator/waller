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
        Path = "Models\WallpaperSource.cs"
        PositionalPattern = 'public\s+sealed\s+record\s+WallpaperSource\s*\('
        Required = @(
            "private WallpaperSourceKind kind",
            "Enum.IsDefined(value)",
            "throw new ArgumentOutOfRangeException(nameof(value), value,",
            "TryNormalize(WallpaperSource? source)"
        )
    },
    @{
        Path = "Models\MonitorSession.cs"
        PositionalPattern = $null
        Required = @(
            "private MonitorApplyStatus applyStatus",
            "Enum.IsDefined(value)",
            "throw new ArgumentOutOfRangeException(nameof(value), value,",
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
            "RequiredList.Copy(",
            "Preset assignment list cannot include null items."
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
        Path = "Presets\PresetStore.cs"
        PositionalPattern = $null
        Required = @(
            "LocalDataRootDirectory.RequireFullyQualified(rootDirectory)"
        )
    },
    @{
        Path = "Settings\UserSettingsStore.cs"
        PositionalPattern = $null
        Required = @(
            "LocalDataRootDirectory.RequireFullyQualified(rootDirectory)"
        )
    },
    @{
        Path = "Rendering\RenderedWallpaperStore.cs"
        PositionalPattern = $null
        Required = @(
            "LocalDataRootDirectory.RequireFullyQualified(rootDirectory)"
        )
    },
    @{
        Path = "Settings\UserSettings.cs"
        PositionalPattern = $null
        Required = @(
            "Enum.IsDefined(theme)",
            "Theme preference is not supported.",
            "AppLanguages.Normalize(language)",
            "Settings language is not supported.",
            "Language = normalizedLanguage",
            "Math.Max(UserSettingsPolicy.MinWindowWidth, width)",
            "Math.Max(UserSettingsPolicy.MinWindowHeight, height)"
        )
    },
    @{
        Path = "Sessions\ApplyProgress.cs"
        PositionalPattern = $null
        Required = @(
            "private MonitorApplyStatus status",
            "Enum.IsDefined(value)",
            "throw new ArgumentOutOfRangeException(nameof(value), value,",
            "Monitor apply status is invalid"
        )
    },
    @{
        Path = "Sessions\ApplyRunTracker.cs"
        PositionalPattern = $null
        Required = @(
            "private void RecordCompletedStep()",
            "completed >= total",
            "Apply tracker cannot record more completed steps than its total.",
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
