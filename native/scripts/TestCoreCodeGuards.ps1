param(
    [string]$CorePath = ".\Waller.Native.Core",
    [string]$TestsPath = ".\Waller.Native.Tests"
)

$ErrorActionPreference = "Stop"
$nativeRoot = Split-Path -Parent $PSScriptRoot
$resolvedCorePath = if ([System.IO.Path]::IsPathRooted($CorePath)) {
    $CorePath
}
else {
    Join-Path $nativeRoot $CorePath
}
$resolvedTestsPath = if ([System.IO.Path]::IsPathRooted($TestsPath)) {
    $TestsPath
}
else {
    Join-Path $nativeRoot $TestsPath
}

foreach ($requiredPath in @($resolvedCorePath, $resolvedTestsPath)) {
    if (-not (Test-Path -LiteralPath $requiredPath)) {
        throw "Core boundary input not found: $requiredPath"
    }
}

$projectPath = Join-Path $resolvedCorePath "Waller.Native.Core.csproj"
$project = Get-Content -LiteralPath $projectPath -Raw
$errors = @()

if ($project -match '<ProjectReference\b' -or $project -match '<PackageReference\b') {
    $errors += "Core must remain a dependency root without project or package references."
}

$forbiddenSourcePatterns = @(
    'using\s+Waller\.Native\.(App|Workflows|Tests)',
    'Microsoft\.UI\.',
    'Windows\.UI\.',
    'CommunityToolkit\.',
    '\[(Fact|Theory)\]'
)
$coreSources = Get-ChildItem -LiteralPath $resolvedCorePath -Recurse -Filter *.cs |
    Where-Object { $_.FullName -notmatch '\\(bin|obj)\\' }
foreach ($source in $coreSources) {
    $text = Get-Content -LiteralPath $source.FullName -Raw
    foreach ($pattern in $forbiddenSourcePatterns) {
        if ($text -match $pattern) {
            $relativePath = [System.IO.Path]::GetRelativePath($resolvedCorePath, $source.FullName)
            $errors += "Core boundary leak in $relativePath`: $pattern"
        }
    }
}

if (Get-ChildItem -LiteralPath $resolvedCorePath -Recurse -Filter "*SampleMonitor*.cs") {
    $errors += "Deterministic sample monitors must not ship in Core."
}

$fixturePath = Join-Path $resolvedTestsPath "Fixtures\SampleMonitorDetector.cs"
if (-not (Test-Path -LiteralPath $fixturePath)) {
    $errors += "Tests must own the deterministic SampleMonitorDetector fixture."
}

$legacyMonolith = Join-Path $resolvedTestsPath "CoreArchitectureTests.cs"
if (Test-Path -LiteralPath $legacyMonolith) {
    $errors += "The monolithic CoreArchitectureTests.cs must stay split by domain."
}

$requiredTestModules = @(
    "Core\Apply\ApplyCoreTests.cs",
    "Core\Models\WallpaperModelTests.cs",
    "Core\Presets\PresetCoreTests.cs",
    "Core\Rendering\RenderingCoreTests.cs",
    "Core\Sessions\SessionCoreTests.cs",
    "Core\Settings\SettingsCoreTests.cs",
    "Core\Storage\StorageCoreTests.cs",
    "Core\Topology\TopologyCoreTests.cs",
    "Core\Windows\WindowsAdapterTests.cs"
)
foreach ($relativePath in $requiredTestModules) {
    if (-not (Test-Path -LiteralPath (Join-Path $resolvedTestsPath $relativePath))) {
        $errors += "Core test module missing: $relativePath"
    }
}

if ($errors.Count -gt 0) {
    foreach ($boundaryError in $errors) {
        Write-Host "CORE BOUNDARY ERROR: $boundaryError" -ForegroundColor Red
    }

    exit 1
}

Write-Host "Core dependency and test-ownership guards passed."
