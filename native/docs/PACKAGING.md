# Waller Native Packaging

Run commands from `native\`.

## Golden Gates

Fast development gate:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\Verify.ps1 -SkipSmoke
```

Offline/sandbox gate without NuGet vulnerability-audit network warnings:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\Verify.ps1 -SkipSmoke -DisableNuGetAudit
```

Full launch gate:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\Verify.ps1
```

Fast package gate:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\Verify.ps1 -SkipSmoke -Package
```

Release-only gate:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\Verify.ps1 -SkipSmoke -Release
```

## Script Map

| Script | Purpose | Mutates machine state |
| --- | --- | --- |
| `scripts\Verify.ps1` | Runs lint/build/test/smoke/package gates. | Only when smoke registers debug package. |
| `scripts\TestModalKeyboardContract.ps1` | Verifies modal Escape close and initial focus routing for Save As, Manage Presets, Settings, and delete confirmation. | No. |
| `scripts\TestShellCommandContract.ps1` | Verifies top-shell command bindings, command gating, keyboard accelerators, and horizontal overflow handling. | No. |
| `scripts\SmokeLaunch.ps1` | Builds, launches packaged app, verifies process/title, closes app. On registration conflict, runs the read-only package registration diagnostic automatically. | Registers debug package through `winapp run`. |
| `scripts\SmokeSurface.ps1` | Builds, launches packaged app, verifies process/title/responding, checks shell and modal controls through UI Automation, optionally verifies Settings JSON roundtrip, closes app. | Registers debug package through `winapp run`; with `-SettingsRoundTrip`, temporarily writes and restores package-local `LocalCache\Local\Waller\settings.json`. |
| `scripts\SmokeApply.ps1` | Builds, launches packaged app, invokes Apply all through UI Automation, verifies rendered PNGs, wallpaper path changes, and restores the previous wallpapers. | Temporarily changes current-user wallpapers, then restores them in `finally`. |
| `scripts\BuildRelease.ps1` | Builds Release without launching/signing. | No. |
| `scripts\PrepareDevCertificate.ps1` | Generates local dev PFX/CER. | Writes ignored files under `artifacts\signing\`. |
| `scripts\PackageDevMsix.ps1` | Builds Release and creates signed dev MSIX. | Writes ignored files under `artifacts\packages\`. |
| `scripts\InspectDevMsix.ps1` | Verifies MSIX manifest/signature. | No. |
| `scripts\PackageManifest.ps1` | Shared helpers for manifest path resolution, MSIX manifest reads, package identity resolution, and MSIX version validation. | No. |
| `scripts\PackageRegistration.ps1` | Shared read-only helpers for current-user/all-user package registration lookup and display formatting. | No. |
| `scripts\SetPackageVersion.ps1` | Reads or changes package manifest version. | Only with `-Version`. |
| `scripts\TestDevCertificateTrust.ps1` | Checks cert trust stores. | No. |
| `scripts\TestDevPackageRegistration.ps1` | Checks current/all-user package registrations from explicit, MSIX, or source manifest identity. | No. |
| `scripts\TestPackageDiagnosticBehavior.ps1` | Read-only regression checks for package registration diagnostics, including non-elevated all-users behavior. | No. |
| `scripts\TestPackageAssets.ps1` | Verifies package identity/version text plus manifest/window asset references. | No. |
| `scripts\TestLaunchContract.ps1` | Verifies stable AUMID suffix, Waller window title, and `winapp`-based smoke-launch contract. | No. |
| `scripts\TestSigningPolicy.ps1` | Verifies signing docs, ignored cert patterns, and that local cert artifacts stay under `artifacts\signing\`. | No. |
| `scripts\TestLocalDataPolicy.ps1` | Verifies Presets/Settings stay package-local while rendered PNGs use a shell-readable user-profile cache. | No. |
| `scripts\TestPackageUpdatePolicy.ps1` | Verifies version updates do not change package identity and docs keep update behavior tied to stable local app data. | No. |
| `scripts\TestPackageScriptGuards.ps1` | Blocks direct package manifest reads outside shared helpers, package registration lookups outside `PackageRegistration.ps1`, package installs outside `InstallDevMsix.ps1`, and package removal outside `UninstallDevPackage.ps1`. | No. |
| `scripts\InstallDevMsix.ps1` | Inspects MSIX, checks current-user registration and cert trust; installs only with `-Install`. | Only with `-Install`. |
| `scripts\UninstallDevPackage.ps1` | Lists installed dev package from explicit, MSIX, or source manifest identity; supports current-user or elevated all-user cleanup; removes only with `-Uninstall`. | Only with `-Uninstall`. |
| `scripts\FindWinApp.ps1` | Finds `winapp.exe` in PATH or NuGet cache. | No. |

## Local App Data

Waller keeps app-managed data under one local app-data root:

```text
Waller
```

That root contains Presets and Settings. In packaged WinUI runs, Windows
resolves `Environment.SpecialFolder.LocalApplicationData` to the package family
`LocalCache\Local`, so the effective path is:

```text
%LOCALAPPDATA%\Packages\<package-family-name>\LocalCache\Local\Waller
```

Package version updates preserve this data as long as package name and publisher
stay stable. `TestLocalDataPolicy.ps1` guards shared store construction before
build/package gates.

Rendered wallpaper PNGs intentionally do not live under package-local AppData:
Windows `IDesktopWallpaper` cannot read those virtualized files. Runtime render
output goes under:

```text
%USERPROFILE%\.waller\rendered
```

`SmokeApply.ps1` verifies that packaged Apply writes there, changes all current
wallpaper paths, then restores the previous wallpaper state.

## Launch Contract

Packaged launch must use package identity, not the generated `.exe` directly:

- Package `Application Id` stays `App`, producing stable AUMID suffix `!App`.
- Main window and title bar titles stay `Waller`.
- `BuildAndRun.ps1` launches through `winapp run`.
- `SmokeLaunch.ps1` uses detached `winapp` JSON, verifies process
  `Waller.Native.App`, title `Waller`, `Responding=True`, and cleans up the
  launched process.
- `Verify.ps1 -SurfaceSmoke` runs `SmokeSurface.ps1` after the default launch
  smoke, keeping UI Automation surface coverage opt-in for slower local gates.
- `Verify.ps1 -SurfaceSmoke -SettingsRoundTrip` additionally proves packaged
  Settings persistence through UI Automation while restoring the previous
  package-local Settings file after the smoke run.
- `TestLaunchContract.ps1` guards these launch-contract assumptions before
  build/package gates. Real Start-menu launch smoke still requires a successful
  package registration.

## Update Policy

Updates preserve user data by separating package update metadata from local app
data:

- `SetPackageVersion.ps1 changes only Identity.Version`; package name and
  publisher stay stable.
- Presets/settings live under package `LocalCache\Local\Waller` for packaged
  runs.
- Rendered PNGs live under `%USERPROFILE%\.waller\rendered` so the Windows
  wallpaper shell can read them.
- Package name and publisher stay stable so version updates keep the same
  package family local cache.
- `TestPackageUpdatePolicy.ps1` guards the version-update script and this policy
  before build/package gates.

## Versioning

Read current package identity without mutating files:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\SetPackageVersion.ps1
```

Change MSIX package version:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\SetPackageVersion.ps1 -Version 1.0.1.0
```

Rules:

- MSIX version must use four numeric parts: `major.minor.build.revision`.
- Each part must be between `0` and `65535`.
- Do not use SemVer suffixes such as `-beta`.
- Version lives in `Waller.Native.App\Package.appxmanifest`.
- `Verify.ps1` also fails if the manifest version is malformed, so manual
  manifest edits cannot bypass `SetPackageVersion.ps1` validation.
- After changing version, run at least:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\Verify.ps1 -SkipSmoke
```

Before creating a dev package for handoff/testing, prefer:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\Verify.ps1 -SkipSmoke -Package
```

## Signing Strategy

Development signing:

- Use the generated local `CN=Waller` dev certificate only for internal MVP
  handoff and local package install tests.
- Keep generated `.pfx` and `.cer` files under ignored `artifacts\signing\`.
- Trusting the dev certificate is an intentional elevated/manual step; package
  creation must not install or trust certificates.
- Dev packages are useful for launch/install validation, not public release.

Release signing:

- Production distribution needs a real production certificate decision before
  any user-facing MSIX handoff.
- Production packages should use a timestamping service so signatures survive
  certificate expiry.
- Production signing material must stay outside the repository and outside
  `artifacts\signing\`; only metadata and commands belong in docs/scripts.
- Do not enable trimming or call a package release-ready until clean-profile
  install, Start-menu launch, update, uninstall, and wallpaper Apply smoke pass.

## Development Certificate

Generate without trust/install:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\PrepareDevCertificate.ps1
```

Local generated certificate:

```text
Subject: CN=Waller
Thumbprint: 3403C36B7E93446F269873593B379EC2419D5F17
```

Generated files are ignored:

```text
artifacts\signing\devcert.pfx
artifacts\signing\devcert.cer
```

Trusting certificate is a separate elevated step:

```powershell
winapp cert install .\artifacts\signing\devcert.pfx
```

For a copy/paste-safe elevated command with absolute paths, run:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\TestDevCertificateTrust.ps1
```

`PrepareDevCertificate.ps1`, `PackageDevMsix.ps1`, and
`TestDevCertificateTrust.ps1` all print the resolved PFX path in the trust
command.

## Development MSIX

Build signed dev package without install:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\PackageDevMsix.ps1
```

Output:

```text
artifacts\packages\Waller-dev-x64.msix
```

Expected signature before trusting the dev cert:

```text
Signature: UnknownError / 3403C36B7E93446F269873593B379EC2419D5F17
```

`UnknownError` is expected until the dev certificate is trusted.

Latest local package gate:

```text
2026-06-08
Command: powershell -ExecutionPolicy Bypass -File .\scripts\Verify.ps1 -SkipSmoke -Package -DisableNuGetAudit
Result: VERIFY PASSED
Output: artifacts\packages\Waller-dev-x64.msix
Size: 78,682,578 bytes
Identity Name: 1EB1FFC3-B778-402F-85FA-F6C6BF1EA9A4
Publisher: CN=Waller
Version: 1.0.0.0
Architecture: x64
Signature: UnknownError / 3403C36B7E93446F269873593B379EC2419D5F17
Trust: DEV CERT NOT TRUSTED
```

Latest refreshed local package gate:

```text
2026-06-08
Command: powershell -ExecutionPolicy Bypass -File .\scripts\Verify.ps1 -SkipSmoke -Package -DisableNuGetAudit
Result: VERIFY PASSED
Output: artifacts\packages\Waller-dev-x64.msix
Size: 78,687,150 bytes
Identity Name: 1EB1FFC3-B778-402F-85FA-F6C6BF1EA9A4
Publisher: CN=Waller
Version: 1.0.0.0
Architecture: x64
Signature: UnknownError / 3403C36B7E93446F269873593B379EC2419D5F17
Trust: DEV CERT NOT TRUSTED
Tests: Passed 149 / Failed 0 / Skipped 0
```

Latest current package gate after local-state and guard prefactors:

```text
2026-06-08
Command: powershell -ExecutionPolicy Bypass -File .\scripts\Verify.ps1 -SkipSmoke -Package -DisableNuGetAudit
Result: VERIFY PASSED
Output: artifacts\packages\Waller-dev-x64.msix
Size: 78,698,590 bytes
Identity Name: 1EB1FFC3-B778-402F-85FA-F6C6BF1EA9A4
Publisher: CN=Waller
Version: 1.0.0.0
Architecture: x64
Signature: UnknownError / 3403C36B7E93446F269873593B379EC2419D5F17
Trust: DEV CERT NOT TRUSTED
Tests: Passed 160 / Failed 0 / Skipped 0
Trust command: C:\Users\cristian\.nuget\packages\microsoft.windows.sdk.buildtools.winapp\0.3.2\tools\win-x64\winapp.exe cert install D:\DEV\waller\native\artifacts\signing\devcert.pfx
```

In restricted sandbox mode, Release restore may fail with `NU1301` socket
permission errors. Run the same command outside the restricted sandbox when
validating package output.

## Install / Uninstall

Install preflight without install:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\InstallDevMsix.ps1
```

The preflight inspects the MSIX and reads the package identity from the package
itself before checking current-user registration. It blocks when that same
development package is already registered for the current user. Run the
uninstall preflight first, then remove intentionally with `-Uninstall` if
cleanup is desired. Use `-SkipRegistrationCheck` only when you intentionally
want to bypass this guard.

Optional all-user registration preflight:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\InstallDevMsix.ps1 -AllUsersRegistrationCheck
```

This may need elevation. If the all-user check is inconclusive, install preflight
blocks before cert trust/install.

Install only after certificate trust:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\InstallDevMsix.ps1 -Install
```

Check installed dev package:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\TestDevPackageRegistration.ps1
```

Check registration for a specific MSIX artifact:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\TestDevPackageRegistration.ps1 -PackagePath .\artifacts\packages\Waller-dev-x64.msix
```

Check package registration across users when launch reports `0x80073D19`:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\TestDevPackageRegistration.ps1 -AllUsers
```

When not elevated, combined `-AllUsers` mode skips current-user lookup to avoid
misreporting current-user state. Run the script without `-AllUsers` for
current-user-only preflight, then rerun `-AllUsers` from an elevated terminal
for the conclusive all-user check.
`SmokeLaunch.ps1` runs those two read-only diagnostics separately when `winapp`
reports `0x80073D19`.
`SmokeLaunch.ps1`, `TestDevPackageRegistration.ps1`, and
`UninstallDevPackage.ps1` share the same package-conflict help from
`scripts\PackageRegistration.ps1`, so the `0x80073D19` path always prints exact
read-only diagnostics first and treats `-Uninstall` as explicit cleanup.

Exit codes:

- `0`: no current-user registration found, and all-user check passed or was not requested.
- `2`: current-user registration exists; cleanup may be needed before smoke launch.
- `3`: current user is clean, but all-user check was blocked by permissions.

Legacy cleanup preflight:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\UninstallDevPackage.ps1
```

By default this reads package `Identity Name` from
`Waller.Native.App\Package.appxmanifest`, so package-name changes do not require
updating the uninstall script. Prefer `-PackagePath` when cleanup should target
the identity from a specific MSIX artifact:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\UninstallDevPackage.ps1 -PackagePath .\artifacts\packages\Waller-dev-x64.msix
```

Use `-PackageName` only for explicit cleanup of a different identity.

Remove installed dev package:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\UninstallDevPackage.ps1 -Uninstall
```

Elevated all-user cleanup preflight for `0x80073D19` conflicts:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\UninstallDevPackage.ps1 -AllUsers
```

If all-user inspection is denied, re-run from an elevated terminal. Remove
all-user dev registrations only when intentional and from an elevated terminal:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\UninstallDevPackage.ps1 -AllUsers -Uninstall
```

Current package identity:

```text
Name: 1EB1FFC3-B778-402F-85FA-F6C6BF1EA9A4
Publisher: CN=Waller
Version: 1.0.0.0
```

## Release Notes

Release builds currently keep trimming disabled. JSON persistence uses
source-generated metadata, and manual COM activation has a documented trim
analysis suppression. Remaining trim risk sits in external WinRT assemblies and
needs a separate trimmed launch/apply validation before enabling trimming.

`-DisableNuGetAudit` is available on local build scripts for restricted-network
environments where NuGet vulnerability data cannot be reached and causes
`NU1900` warnings. It is opt-in; default gates keep NuGet audit behavior.
Release/package scripts also fail on nested child-process exit codes instead of
trusting console output alone.

Signed production distribution still needs:

- production certificate decision
- timestamping
- clean user profile install smoke
- update behavior smoke
- uninstall behavior smoke
