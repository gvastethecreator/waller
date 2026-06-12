param(
    [string]$PackageName = "Waller.Nonexistent.Package.Diagnostics"
)

$ErrorActionPreference = "Stop"

$nativeRoot = Split-Path -Parent $PSScriptRoot

. "$PSScriptRoot\PackageRegistration.ps1"

function Invoke-DiagnosticScript {
    param([string[]]$Arguments)

    Push-Location $nativeRoot
    try {
        $global:LASTEXITCODE = 0
        $output = powershell -ExecutionPolicy Bypass @Arguments 2>&1
        return [pscustomobject]@{
            ExitCode = $LASTEXITCODE
            Text = $output | Out-String
        }
    }
    finally {
        Pop-Location
    }
}

function Assert-Contains {
    param(
        [string]$Text,
        [string]$Pattern,
        [string]$Message
    )

    if ($Text -notmatch [regex]::Escape($Pattern)) {
        Write-Host $Message -ForegroundColor Red
        Write-Host $Text
        exit 1
    }
}

function Assert-NotContains {
    param(
        [string]$Text,
        [string]$Pattern,
        [string]$Message
    )

    if ($Text -match [regex]::Escape($Pattern)) {
        Write-Host $Message -ForegroundColor Red
        Write-Host $Text
        exit 1
    }
}

$currentUser = Invoke-DiagnosticScript -Arguments @(
    "-File",
    ".\scripts\TestDevPackageRegistration.ps1",
    "-PackageName",
    $PackageName)

if ($currentUser.ExitCode -ne 0) {
    Write-Host "Current-user package diagnostic should be read-only and pass for an absent explicit package." -ForegroundColor Red
    Write-Host $currentUser.Text
    exit 1
}

Assert-Contains `
    -Text $currentUser.Text `
    -Pattern "CURRENT USER PACKAGE NOT REGISTERED: $PackageName" `
    -Message "Current-user package diagnostic did not report absent explicit package."

if (-not (Test-WallerProcessIsElevated)) {
    $allUsers = Invoke-DiagnosticScript -Arguments @(
        "-File",
        ".\scripts\TestDevPackageRegistration.ps1",
        "-PackageName",
        $PackageName,
        "-AllUsers")

    if ($allUsers.ExitCode -ne 3) {
        Write-Host "Non-elevated all-users package diagnostic should exit 3." -ForegroundColor Red
        Write-Host $allUsers.Text
        exit 1
    }

    Assert-Contains `
        -Text $allUsers.Text `
        -Pattern "CURRENT USER PACKAGE CHECK SKIPPED IN COMBINED ALL-USERS MODE." `
        -Message "Non-elevated all-users diagnostic should skip current-user lookup."
    Assert-Contains `
        -Text $allUsers.Text `
        -Pattern "All-user package inspection requires an elevated terminal." `
        -Message "Non-elevated all-users diagnostic should name elevation requirement."
    Assert-Contains `
        -Text $allUsers.Text `
        -Pattern "PACKAGE REGISTRATION CONFLICT HELP:" `
        -Message "Non-elevated all-users diagnostic should print conflict help."
    Assert-Contains `
        -Text $allUsers.Text `
        -Pattern "powershell -ExecutionPolicy Bypass -File .\scripts\TestDevPackageRegistration.ps1 -PackageName `"$PackageName`" -AllUsers" `
        -Message "Non-elevated all-users diagnostic should print exact all-users diagnostic command."
    Assert-NotContains `
        -Text $allUsers.Text `
        -Pattern "CategoryInfo" `
        -Message "Non-elevated all-users diagnostic should not dump PowerShell error details."

    $uninstall = Invoke-DiagnosticScript -Arguments @(
        "-File",
        ".\scripts\UninstallDevPackage.ps1",
        "-PackageName",
        $PackageName,
        "-AllUsers")

    if ($uninstall.ExitCode -ne 3) {
        Write-Host "Non-elevated all-users uninstall preflight should exit 3." -ForegroundColor Red
        Write-Host $uninstall.Text
        exit 1
    }

    Assert-Contains `
        -Text $uninstall.Text `
        -Pattern "All-user package inspection requires an elevated terminal." `
        -Message "Non-elevated all-users uninstall preflight should name elevation requirement."
    Assert-Contains `
        -Text $uninstall.Text `
        -Pattern "Only add -Uninstall when removing the dev package is intentional." `
        -Message "Non-elevated all-users uninstall preflight should warn before cleanup."
    Assert-NotContains `
        -Text $uninstall.Text `
        -Pattern "CategoryInfo" `
        -Message "Non-elevated all-users uninstall preflight should not dump PowerShell error details."
}
else {
    Write-Host "Skipping non-elevated all-users diagnostic assertions in elevated shell." -ForegroundColor Yellow
}

Write-Host "Package diagnostic behavior passed."
