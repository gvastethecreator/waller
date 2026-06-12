param(
    [string]$ProjectPath = ".\Waller.Native.App\Waller.Native.App.csproj",
    [int]$LaunchTimeoutSeconds = 8,
    [switch]$DisableNuGetAudit
)

$ErrorActionPreference = "Stop"

$nativeRoot = Split-Path -Parent $PSScriptRoot
$buildScript = Join-Path $nativeRoot "BuildAndRun.ps1"
$appProcessId = $null

. "$PSScriptRoot\PackageRegistration.ps1"

function Assert-LastExitCode {
    param([string]$Step)

    if ($null -ne $LASTEXITCODE -and $LASTEXITCODE -ne 0) {
        throw "$Step failed with exit code $LASTEXITCODE."
    }
}

function Stop-LaunchedApp {
    param([int]$ProcessId)

    $process = Get-Process -Id $ProcessId -ErrorAction SilentlyContinue
    if (-not $process) {
        return
    }

    $null = $process.CloseMainWindow()
    Start-Sleep -Seconds 1
    $process = Get-Process -Id $ProcessId -ErrorAction SilentlyContinue
    if ($process) {
        Stop-Process -Id $ProcessId -Force
    }
}

Push-Location $nativeRoot
try {
    $buildArgs = @($ProjectPath, "-Detach")
    if ($DisableNuGetAudit) {
        $buildArgs += "-DisableNuGetAudit"
    }

    $output = powershell -ExecutionPolicy Bypass -File $buildScript @buildArgs 2>&1
    Assert-LastExitCode "Packaged launch"
    $text = $output | Out-String
    Write-Host $text

    $jsonMatch = [regex]::Match($text, "(?s)\{.*\}\s*$")
    if (-not $jsonMatch.Success) {
        throw "Launch output did not include trailing winapp JSON."
    }

    $launch = $jsonMatch.Value | ConvertFrom-Json
    if ($launch.Error) {
        if ($launch.Error -match "0x80073D19|conflicting package|already installed") {
            Write-WallerPackageConflictHelp
            Write-Host ""
            Write-Host "Running read-only current-user package registration diagnostic now..." -ForegroundColor Yellow
            $currentUserDiagnostic = powershell -ExecutionPolicy Bypass -File .\scripts\TestDevPackageRegistration.ps1 2>&1
            $currentUserDiagnostic | Out-String | Write-Host
            Write-Host "Running read-only all-users package registration diagnostic now..." -ForegroundColor Yellow
            $allUsersDiagnostic = powershell -ExecutionPolicy Bypass -File .\scripts\TestDevPackageRegistration.ps1 -AllUsers 2>&1
            $allUsersDiagnostic | Out-String | Write-Host
        }

        throw "winapp launch failed: $($launch.Error)"
    }

    if (-not $launch.ProcessId) {
        throw "Launch JSON did not include ProcessId."
    }

    $appProcessId = [int]$launch.ProcessId
    $deadline = (Get-Date).AddSeconds($LaunchTimeoutSeconds)
    $process = $null

    while ((Get-Date) -lt $deadline) {
        $process = Get-Process -Id $appProcessId -ErrorAction SilentlyContinue
        if ($process -and $process.MainWindowTitle) {
            break
        }

        Start-Sleep -Milliseconds 250
    }

    $process = Get-Process -Id $appProcessId -ErrorAction SilentlyContinue
    if (-not $process) {
        throw "Launched process $appProcessId was not found."
    }

    $process | Select-Object Id, ProcessName, MainWindowTitle, Responding | Format-List | Out-String | Write-Host

    if ($process.ProcessName -ne "Waller.Native.App") {
        throw "Unexpected process name: $($process.ProcessName)."
    }

    if ($process.MainWindowTitle -ne "Waller") {
        throw "Unexpected main window title: $($process.MainWindowTitle)."
    }

    if (-not $process.Responding) {
        throw "Launched app is not responding."
    }

    Stop-LaunchedApp -ProcessId $appProcessId

    Write-Host "SMOKE LAUNCH PASSED: $appProcessId"
}
finally {
    if ($appProcessId) {
        Stop-LaunchedApp -ProcessId $appProcessId
    }

    Pop-Location
}
