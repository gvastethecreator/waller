param(
    [ValidateSet("x64", "x86", "ARM64")]
    [string]$Platform = "x64",
    [string]$ProjectPath = ".\Waller.Native.App\Waller.Native.App.csproj",
    [switch]$DisableNuGetAudit
)

$ErrorActionPreference = "Stop"

$nativeRoot = Split-Path -Parent $PSScriptRoot
$buildScript = Join-Path $nativeRoot "BuildAndRun.ps1"

function Assert-LastExitCode {
    param([string]$Step)

    if ($null -ne $LASTEXITCODE -and $LASTEXITCODE -ne 0) {
        throw "$Step failed with exit code $LASTEXITCODE."
    }
}

Push-Location $nativeRoot
try {
    $buildArgs = @(
        $ProjectPath,
        "-SkipRun",
        "/p:Configuration=Release",
        "/p:Platform=$Platform"
    )

    if ($DisableNuGetAudit) {
        $buildArgs += "-DisableNuGetAudit"
    }

    powershell -ExecutionPolicy Bypass -File $buildScript `
        @buildArgs
    Assert-LastExitCode "Release build"

    Write-Host ""
    Write-Host "RELEASE BUILD PASSED: $Platform" -ForegroundColor Green
}
finally {
    Pop-Location
}
