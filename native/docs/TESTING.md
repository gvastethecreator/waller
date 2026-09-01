# Waller Native testing

Testing starts in Core and grows outward. Run checks at the end of a task round, not after every small edit. Commands below run from `native/` unless noted.

## Current commands

Full local verification:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\Verify.ps1
```

Runs XAML accessibility lint, XAML localization lint, WinUI code guards, JSON code guards, solution build, tests, and packaged launch smoke. The smoke step includes the packaged build. Any non-zero child exit code fails verification.

Without NuGet audit warnings:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\Verify.ps1 -DisableNuGetAudit
```

Packaged UI Automation surface smoke:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\Verify.ps1 -SurfaceSmoke -DisableNuGetAudit
```

`-SkipSmoke` disables both launch and surface smoke.

Settings roundtrip plus surface smoke:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\Verify.ps1 -SurfaceSmoke -SettingsRoundTrip -DisableNuGetAudit
```

This saves Settings through the packaged app and verifies package-local `LocalCache\Local\Waller\settings.json`. The smoke backs up and restores the previous Settings file before exit.

Apply smoke:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\Verify.ps1 -SurfaceSmoke -SettingsRoundTrip -ApplySmoke -DisableNuGetAudit
```

Apply smoke captures current wallpapers, invokes Apply all, verifies rendered PNGs under `%USERPROFILE%\.waller\rendered`, then restores the previous image paths or the all-solid-color desktop state. Run it only when that desktop mutation is acceptable.

Without launch smoke:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\Verify.ps1 -SkipSmoke
```

Restricted-network/offline version:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\Verify.ps1 -SkipSmoke -DisableNuGetAudit
```

Release and package gates:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\Verify.ps1 -SkipSmoke -Release
powershell -ExecutionPolicy Bypass -File .\scripts\Verify.ps1 -SkipSmoke -Package
```

From the repository root:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\Invoke-Native.ps1 -Task Verify -SkipSmoke
```

## Test pyramid

1. Core unit tests for models, rendering, persistence, and Apply preflight.
2. Workflow tests on public seams with fake Windows adapters.
3. Static guards for XAML accessibility, localization, WinUI, JSON, error text, MVP scope, and package policy.
4. Packaged launch, surface, Settings, and Apply smoke when the changed risk needs them.

Sample monitors exist only in the Tests fixture assembly. Product runtime uses the real Windows detector.

## Fake adapters

Windows COM reads and writes stay behind internal adapters so mapping can be tested without touching the desktop. Do not call `IDesktopWallpaper` from tests.

## Apply smoke warning

`SmokeApply.ps1` temporarily changes the current user's wallpapers and restores them in `finally`. Mixed image and solid-color monitor states are rejected before mutation.

## CI

CI runs the native verification gate without Apply smoke. See [`.github/workflows/ci.yml`](../../.github/workflows/ci.yml).

Packaging, certificates, and install/uninstall commands live in [`PACKAGING.md`](PACKAGING.md).
