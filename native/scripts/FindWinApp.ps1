function Find-WinApp {
    param(
        [string]$RuntimeIdentifier = "win-x64"
    )

    $winapp = Get-Command winapp -ErrorAction SilentlyContinue
    if ($winapp) {
        return $winapp.Source
    }

    $packageRoot = Join-Path $env:USERPROFILE ".nuget\packages\microsoft.windows.sdk.buildtools.winapp"
    if (-not (Test-Path $packageRoot)) {
        return $null
    }

    $cached = Get-ChildItem -Path $packageRoot -Recurse -Filter "winapp.exe" |
        Where-Object { $_.FullName -like "*\tools\$RuntimeIdentifier\winapp.exe" } |
        Sort-Object FullName -Descending |
        Select-Object -First 1

    if ($cached) {
        return $cached.FullName
    }

    return $null
}
