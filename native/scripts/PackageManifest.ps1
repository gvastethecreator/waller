$script:WallerNativeRoot = Split-Path -Parent $PSScriptRoot

function Get-WallerNativeRoot {
    return $script:WallerNativeRoot
}

function Resolve-WallerNativePath {
    param([string]$Path)

    if ([System.IO.Path]::IsPathRooted($Path)) {
        return $Path
    }

    return Join-Path $script:WallerNativeRoot $Path
}

function Test-WallerMsixVersion {
    param([string]$Value)

    if ([string]::IsNullOrWhiteSpace($Value) -or $Value -notmatch '^\d+\.\d+\.\d+\.\d+$') {
        return $false
    }

    foreach ($part in $Value.Split(".")) {
        $number = 0
        if (-not [int]::TryParse($part, [ref]$number)) {
            return $false
        }

        if ($number -lt 0 -or $number -gt 65535) {
            return $false
        }
    }

    return $true
}

function Read-WallerPackageManifest {
    param([string]$ManifestPath)

    $manifestFullPath = Resolve-WallerNativePath $ManifestPath
    if (-not (Test-Path -LiteralPath $manifestFullPath)) {
        throw "Package manifest not found: $ManifestPath"
    }

    return [xml](Get-Content -LiteralPath $manifestFullPath -Raw)
}

function Read-WallerMsixManifest {
    param([string]$PackagePath)

    $packageFullPath = Resolve-WallerNativePath $PackagePath
    if (-not (Test-Path -LiteralPath $packageFullPath)) {
        throw "MSIX package not found: $PackagePath"
    }

    Add-Type -AssemblyName System.IO.Compression.FileSystem
    $zip = [System.IO.Compression.ZipFile]::OpenRead((Resolve-Path -LiteralPath $packageFullPath))
    try {
        $manifestEntry = $zip.GetEntry("AppxManifest.xml")
        if (-not $manifestEntry) {
            throw "AppxManifest.xml not found in MSIX package."
        }

        $reader = [IO.StreamReader]::new($manifestEntry.Open())
        try {
            return [xml]$reader.ReadToEnd()
        }
        finally {
            $reader.Dispose()
        }
    }
    finally {
        $zip.Dispose()
    }
}

function Get-WallerPackageIdentity {
    param(
        [string]$PackageName,
        [string]$PackagePath,
        [string]$ManifestPath = ".\Waller.Native.App\Package.appxmanifest"
    )

    if ($PackageName) {
        return [pscustomobject]@{
            Name = $PackageName
            Publisher = $null
            Version = $null
            Source = "explicit"
        }
    }

    [xml]$manifest = if ($PackagePath) {
        Read-WallerMsixManifest -PackagePath $PackagePath
    }
    else {
        Read-WallerPackageManifest -ManifestPath $ManifestPath
    }

    $identity = $manifest.Package.Identity
    if (-not $identity -or -not $identity.Name) {
        throw "Package manifest has no Identity Name."
    }

    return [pscustomobject]@{
        Name = $identity.Name
        Publisher = $identity.Publisher
        Version = $identity.Version
        Source = if ($PackagePath) { "msix" } else { "manifest" }
    }
}
