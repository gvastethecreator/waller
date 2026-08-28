param(
    [ValidateSet("Verify", "Run", "Release", "Package")]
    [string]$Task = "Verify",
    [ValidateSet("x64", "x86", "ARM64")]
    [string]$Platform = "x64",
    [switch]$SkipSmoke,
    [switch]$SurfaceSmoke,
    [switch]$SettingsRoundTrip,
    [switch]$ApplySmoke,
    [switch]$ReleaseBuild,
    [switch]$SkipRun,
    [switch]$Detach,
    [switch]$DisableNuGetAudit
)

$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $PSScriptRoot
$nativeRoot = Join-Path $repoRoot "native"
$sdkSpec = Get-Content -LiteralPath (Join-Path $repoRoot "global.json") -Raw | ConvertFrom-Json
$sdkVersion = [string]$sdkSpec.sdk.version
$localSdkRoot = Join-Path $repoRoot ".scratch\toolchains\dotnet-$sdkVersion"
$localDotnet = Join-Path $localSdkRoot "dotnet.exe"

if (Test-Path -LiteralPath $localDotnet) {
    $env:DOTNET_ROOT = $localSdkRoot
    $env:PATH = "$localSdkRoot;$env:PATH"
}

Push-Location $repoRoot
try {
    $resolvedSdk = & dotnet --version
    if ($LASTEXITCODE -ne 0) {
        throw "The required .NET SDK is unavailable. Run .\scripts\BootstrapDotnet.ps1."
    }

    Write-Host "Using .NET SDK $resolvedSdk" -ForegroundColor DarkGray

    $taskArgs = @()
    if ($DisableNuGetAudit) { $taskArgs += "-DisableNuGetAudit" }

    switch ($Task) {
        "Verify" {
            if ($SkipSmoke) { $taskArgs += "-SkipSmoke" }
            if ($SurfaceSmoke) { $taskArgs += "-SurfaceSmoke" }
            if ($SettingsRoundTrip) { $taskArgs += "-SettingsRoundTrip" }
            if ($ApplySmoke) { $taskArgs += "-ApplySmoke" }
            if ($ReleaseBuild) { $taskArgs += "-Release" }
            $taskArgs += @("-Platform", $Platform)
            $scriptPath = Join-Path $nativeRoot "scripts\Verify.ps1"
        }
        "Run" {
            $taskArgs += ".\Waller.Native.App\Waller.Native.App.csproj"
            if ($SkipRun) { $taskArgs += "-SkipRun" }
            if ($Detach) { $taskArgs += "-Detach" }
            $taskArgs += @("/p:Platform=$Platform", "/p:SelfContained=true")
            $scriptPath = Join-Path $nativeRoot "BuildAndRun.ps1"
        }
        "Release" {
            $taskArgs += @("-Platform", $Platform)
            $scriptPath = Join-Path $nativeRoot "scripts\BuildRelease.ps1"
        }
        "Package" {
            $taskArgs += @("-Platform", $Platform)
            $scriptPath = Join-Path $nativeRoot "scripts\PackageDevMsix.ps1"
        }
    }

    Push-Location $nativeRoot
    try {
        powershell -ExecutionPolicy Bypass -File $scriptPath @taskArgs
        if ($LASTEXITCODE -ne 0) {
            throw "Native task $Task failed with exit code $LASTEXITCODE."
        }
    }
    finally {
        Pop-Location
    }
}
finally {
    Pop-Location
}
