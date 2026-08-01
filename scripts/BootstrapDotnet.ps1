param(
    [switch]$Force
)

$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $PSScriptRoot
$sdkSpec = Get-Content -LiteralPath (Join-Path $repoRoot "global.json") -Raw | ConvertFrom-Json
$sdkVersion = [string]$sdkSpec.sdk.version
$toolchainRoot = [System.IO.Path]::GetFullPath((Join-Path $repoRoot ".scratch\toolchains"))
$installRoot = [System.IO.Path]::GetFullPath((Join-Path $toolchainRoot "dotnet-$sdkVersion"))
$installerPath = Join-Path $toolchainRoot "dotnet-install.ps1"

if (-not $toolchainRoot.StartsWith($repoRoot, [System.StringComparison]::OrdinalIgnoreCase)) {
    throw "Toolchain path escaped the repository: $toolchainRoot"
}

$dotnetPath = Join-Path $installRoot "dotnet.exe"
if ((Test-Path -LiteralPath $dotnetPath) -and -not $Force) {
    Write-Host "Waller .NET SDK already available: $installRoot" -ForegroundColor Green
    & $dotnetPath --version
    exit $LASTEXITCODE
}

New-Item -ItemType Directory -Path $toolchainRoot -Force | Out-Null
Invoke-WebRequest "https://dot.net/v1/dotnet-install.ps1" -OutFile $installerPath

powershell -ExecutionPolicy Bypass -File $installerPath `
    -Version $sdkVersion `
    -InstallDir $installRoot `
    -Architecture x64 `
    -NoPath

if ($LASTEXITCODE -ne 0) {
    throw "The .NET SDK installer failed with exit code $LASTEXITCODE."
}

& $dotnetPath --version
if ($LASTEXITCODE -ne 0) {
    throw "The installed .NET SDK could not start."
}

Write-Host "Waller .NET SDK ready: $installRoot" -ForegroundColor Green
