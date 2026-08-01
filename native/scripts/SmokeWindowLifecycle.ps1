param(
    [string]$ProjectPath = ".\Waller.Native.App\Waller.Native.App.csproj",
    [int]$LaunchTimeoutSeconds = 10,
    [switch]$SkipBuild,
    [switch]$DisableNuGetAudit
)

$ErrorActionPreference = "Stop"
$nativeRoot = Split-Path -Parent $PSScriptRoot
$buildScript = Join-Path $nativeRoot "BuildAndRun.ps1"
$appProcessId = $null
$settingsPath = $null
$settingsBackupPath = Join-Path ([System.IO.Path]::GetTempPath()) "waller-window-lifecycle-$([guid]::NewGuid().ToString("N")).json"
$hadSettingsFile = $false

. "$PSScriptRoot\FindWinApp.ps1"
. "$PSScriptRoot\PackageManifest.ps1"
. "$PSScriptRoot\PackageRegistration.ps1"

Add-Type -TypeDefinition @"
using System;
using System.Runtime.InteropServices;

public static class WallerWindowSmokeNative
{
    [StructLayout(LayoutKind.Sequential)]
    public struct Rect
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool GetWindowRect(IntPtr window, out Rect rect);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool SetWindowPos(
        IntPtr window,
        IntPtr insertAfter,
        int x,
        int y,
        int width,
        int height,
        uint flags);
}
"@

function Get-WallerOutputDirectory {
    $project = Resolve-Path $ProjectPath
    $projectDirectory = Split-Path -Parent $project
    $platformRoot = Join-Path $projectDirectory "bin\x64\Debug"
    $framework = Get-ChildItem -LiteralPath $platformRoot -Directory |
        Where-Object { $_.Name -match '^net\d' } |
        Sort-Object Name -Descending |
        Select-Object -First 1
    if (-not $framework) {
        throw "Packaged Debug output framework directory not found: $platformRoot"
    }

    $output = Join-Path $framework.FullName "win-x64"
    if (-not (Test-Path -LiteralPath $output)) {
        throw "Packaged Debug output not found: $output"
    }

    return $output
}

function Get-WallerWindowRect([System.Diagnostics.Process]$Process) {
    $Process.Refresh()
    $rect = [WallerWindowSmokeNative+Rect]::new()
    if (-not [WallerWindowSmokeNative]::GetWindowRect($Process.MainWindowHandle, [ref]$rect)) {
        throw "GetWindowRect failed for process $($Process.Id)."
    }

    return [pscustomobject]@{
        X = $rect.Left
        Y = $rect.Top
        Width = $rect.Right - $rect.Left
        Height = $rect.Bottom - $rect.Top
    }
}

function Assert-WallerGeometry {
    param(
        [object]$Actual,
        [object]$Expected,
        [int]$Tolerance = 2
    )

    foreach ($property in @("X", "Y", "Width", "Height")) {
        if ([Math]::Abs([int]$Actual.$property - [int]$Expected.$property) -gt $Tolerance) {
            throw "Window $property was $($Actual.$property); expected $($Expected.$property) +/- $Tolerance."
        }
    }
}

function Set-WallerSettingsProperty {
    param(
        [object]$Settings,
        [string]$Name,
        [object]$Value
    )

    if ($Settings.PSObject.Properties.Name -contains $Name) {
        $Settings.$Name = $Value
    }
    else {
        $Settings | Add-Member -NotePropertyName $Name -NotePropertyValue $Value
    }
}

function Restore-WallerSettings {
    if (-not $settingsPath) {
        return
    }

    if ($script:hadSettingsFile) {
        $settingsDirectory = Split-Path -Parent $settingsPath
        New-Item -ItemType Directory -Path $settingsDirectory -Force | Out-Null
        Copy-Item -LiteralPath $settingsBackupPath -Destination $settingsPath -Force
    }
    elseif (Test-Path -LiteralPath $settingsPath) {
        [System.IO.File]::Delete($settingsPath)
    }

    if (Test-Path -LiteralPath $settingsBackupPath) {
        [System.IO.File]::Delete($settingsBackupPath)
    }
}

Push-Location $nativeRoot
try {
    if (-not $SkipBuild) {
        $buildArgs = @($ProjectPath, "-SkipRun")
        if ($DisableNuGetAudit) {
            $buildArgs += "-DisableNuGetAudit"
        }

        powershell -ExecutionPolicy Bypass -File $buildScript @buildArgs
        if ($LASTEXITCODE -ne 0) {
            throw "Window lifecycle build failed with exit code $LASTEXITCODE."
        }
    }

    $outputDirectory = Get-WallerOutputDirectory
    $winapp = Find-WinApp -RuntimeIdentifier "win-x64"
    if (-not $winapp) {
        throw "winapp CLI not found."
    }

    & $winapp run $outputDirectory --no-launch --json | Out-Null
    if ($LASTEXITCODE -ne 0) {
        throw "winapp debug identity registration failed with exit code $LASTEXITCODE."
    }

    [xml]$manifest = Read-WallerPackageManifest -ManifestPath ".\Waller.Native.App\Package.appxmanifest"
    $packageName = [string]$manifest.Package.Identity.Name
    $package = Get-WallerCurrentUserPackageRegistrations -PackageName $packageName |
        Select-Object -First 1
    if (-not $package) {
        throw "Registered Waller package not found: $packageName"
    }

    $localApplicationData = [Environment]::GetFolderPath("LocalApplicationData")
    $localDataRoot = Join-Path $localApplicationData "Packages\$($package.PackageFamilyName)\LocalCache\Local\Waller"
    $script:settingsPath = Join-Path $localDataRoot "settings.json"
    if (Test-Path -LiteralPath $settingsPath) {
        Copy-Item -LiteralPath $settingsPath -Destination $settingsBackupPath -Force
        $script:hadSettingsFile = $true
        try {
            $seedSettings = Get-Content -LiteralPath $settingsPath -Raw | ConvertFrom-Json
        }
        catch {
            $seedSettings = $null
        }
    }

    if (-not $seedSettings) {
        $seedSettings = [pscustomobject]@{
            Theme = 2
            Language = "en"
            WindowWidth = 1536
            WindowHeight = 1024
            WindowX = $null
            WindowY = $null
            LastSelectedPresetId = $null
            ThemePreferenceWasSet = $false
        }
    }

    Add-Type -AssemblyName System.Windows.Forms
    $workArea = [System.Windows.Forms.Screen]::PrimaryScreen.WorkingArea
    $restoreGeometry = [pscustomobject]@{
        X = $workArea.X + 40
        Y = $workArea.Y + 40
        Width = [Math]::Min(1100, $workArea.Width - 100)
        Height = [Math]::Min(720, $workArea.Height - 100)
    }
    $finalGeometry = [pscustomobject]@{
        X = $workArea.X + 80
        Y = $workArea.Y + 70
        Width = [Math]::Min(1180, $workArea.Width - 140)
        Height = [Math]::Min(760, $workArea.Height - 130)
    }

    foreach ($property in @("X", "Y", "Width", "Height")) {
        Set-WallerSettingsProperty -Settings $seedSettings -Name "Window$property" -Value $restoreGeometry.$property
    }

    New-Item -ItemType Directory -Path $localDataRoot -Force | Out-Null
    $seedSettings | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath $settingsPath -Encoding utf8

    $launchOutput = & $winapp run $outputDirectory --detach --json 2>&1 | Out-String
    if ($LASTEXITCODE -ne 0) {
        throw "winapp lifecycle launch failed with exit code $LASTEXITCODE`: $launchOutput"
    }

    $jsonMatch = [regex]::Match($launchOutput, '(?s)\{.*\}\s*$')
    if (-not $jsonMatch.Success) {
        throw "Lifecycle launch output did not include trailing JSON: $launchOutput"
    }

    $launch = $jsonMatch.Value | ConvertFrom-Json
    $script:appProcessId = [int]$launch.ProcessId
    $deadline = (Get-Date).AddSeconds($LaunchTimeoutSeconds)
    $process = $null
    while ((Get-Date) -lt $deadline) {
        $process = Get-Process -Id $appProcessId -ErrorAction SilentlyContinue
        if ($process -and $process.MainWindowHandle -ne 0) {
            break
        }

        Start-Sleep -Milliseconds 150
    }

    if (-not $process -or $process.MainWindowHandle -eq 0) {
        throw "Lifecycle window was not visible after restore."
    }

    $restored = Get-WallerWindowRect -Process $process
    Assert-WallerGeometry -Actual $restored -Expected $restoreGeometry

    $setFlags = 0x0004 -bor 0x0010
    if (-not [WallerWindowSmokeNative]::SetWindowPos(
        $process.MainWindowHandle,
        [IntPtr]::Zero,
        $finalGeometry.X,
        $finalGeometry.Y,
        $finalGeometry.Width,
        $finalGeometry.Height,
        $setFlags)) {
        throw "SetWindowPos failed for lifecycle smoke."
    }

    Start-Sleep -Milliseconds 300
    $moved = Get-WallerWindowRect -Process $process
    Assert-WallerGeometry -Actual $moved -Expected $finalGeometry

    if (-not $process.CloseMainWindow()) {
        throw "CloseMainWindow did not signal the Waller window."
    }

    if (-not $process.WaitForExit(10000)) {
        throw "Waller process did not exit after the observed close save."
    }

    $script:appProcessId = $null
    $saved = Get-Content -LiteralPath $settingsPath -Raw | ConvertFrom-Json
    $savedGeometry = [pscustomobject]@{
        X = [int]$saved.WindowX
        Y = [int]$saved.WindowY
        Width = [int]$saved.WindowWidth
        Height = [int]$saved.WindowHeight
    }
    Assert-WallerGeometry -Actual $savedGeometry -Expected $finalGeometry

    [pscustomobject]@{
        Restore = "Passed"
        CloseSave = "Passed"
        ProcessExited = $true
        Geometry = "$($savedGeometry.Width)x$($savedGeometry.Height)@$($savedGeometry.X),$($savedGeometry.Y)"
    } | Format-List | Out-String | Write-Host

    Write-Host "WINDOW LIFECYCLE SMOKE PASSED"
}
finally {
    if ($appProcessId) {
        $process = Get-Process -Id $appProcessId -ErrorAction SilentlyContinue
        if ($process) {
            $null = $process.CloseMainWindow()
            if (-not $process.WaitForExit(3000)) {
                Stop-Process -Id $appProcessId -Force
            }
        }
    }

    Restore-WallerSettings
    Pop-Location
}
