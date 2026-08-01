function Test-WallerProcessIsElevated {
    $identity = [Security.Principal.WindowsIdentity]::GetCurrent()
    $principal = [Security.Principal.WindowsPrincipal]::new($identity)
    return $principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
}

function Assert-WallerAllUsersPackageAccess {
    if (-not (Test-WallerProcessIsElevated)) {
        throw "All-user package inspection requires an elevated terminal."
    }
}

function Write-WallerPackageConflictHelp {
    param([string]$PackageName)

    $packageArg = if ([string]::IsNullOrWhiteSpace($PackageName)) { "" } else { " -PackageName `"$PackageName`"" }

    Write-Host "PACKAGE REGISTRATION CONFLICT HELP:" -ForegroundColor Yellow
    Write-Host "Use winapp run or BuildAndRun.ps1 for launch; do not run the packaged .exe directly." -ForegroundColor Yellow
    Write-Host "Read-only current-user diagnostic:" -ForegroundColor Yellow
    Write-Host "powershell -ExecutionPolicy Bypass -File .\scripts\TestDevPackageRegistration.ps1$packageArg" -ForegroundColor Yellow
    Write-Host "Read-only all-users diagnostic (requires elevated terminal):" -ForegroundColor Yellow
    Write-Host "powershell -ExecutionPolicy Bypass -File .\scripts\TestDevPackageRegistration.ps1$packageArg -AllUsers" -ForegroundColor Yellow
    Write-Host "Cleanup preflight:" -ForegroundColor Yellow
    Write-Host "powershell -ExecutionPolicy Bypass -File .\scripts\UninstallDevPackage.ps1$packageArg" -ForegroundColor Yellow
    Write-Host "Only add -Uninstall when removing the dev package is intentional." -ForegroundColor Yellow
}

function Get-WallerCurrentUserPackageRegistrations {
    param([string]$PackageName)

    $userSid = [Security.Principal.WindowsIdentity]::GetCurrent().User.Value
    return @(Get-AppxPackage -User $userSid -Name $PackageName -ErrorAction SilentlyContinue)
}

function Get-WallerAllUserPackageRegistrations {
    param([string]$PackageName)

    Assert-WallerAllUsersPackageAccess
    return @(Get-AppxPackage -AllUsers -Name $PackageName -ErrorAction Stop)
}

function Write-WallerPackageRegistrations {
    param(
        [object[]]$Packages,
        [switch]$AllUsers
    )

    if ($AllUsers) {
        $Packages |
            Select-Object Name, PackageFullName, Publisher, PackageUserInformation |
            Format-List |
            Out-String |
            Write-Host
        return
    }

    $Packages |
        Select-Object Name, PackageFullName, Publisher, InstallLocation |
        Format-List |
        Out-String |
        Write-Host
}
