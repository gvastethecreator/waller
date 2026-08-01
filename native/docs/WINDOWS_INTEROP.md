# Windows Interop Notes

This document describes how Waller Native should interact with Windows APIs.

## Principle

Keep Windows interop behind small Core interfaces:

```text
IMonitorDetector
IWallpaperApplier
IWallpaperRenderer
```

The WinUI app should not call `IDesktopWallpaper`, P/Invoke, or COM directly.

## Current Interop Path

Current implementation uses focused manual COM interop for:

```text
IDesktopWallpaper
```

Manual COM activation uses `Type.GetTypeFromCLSID` plus `Activator`. The
interop factory has an explicit trim-analysis suppression because the COM class
is not constructed through a managed public parameterless constructor. Do not
remove that suppression unless the activation path moves to generated interop.

Implemented:

- `DesktopWallpaperInterop`
- `WindowsMonitorDetector`
- `DesktopWallpaperApplier`

Reason:

CsWin32 package restore was blocked during setup, and detector/apply work should
not wait on network/package access.

## Future CsWin32 Option

Possible future interop path:

```text
Microsoft.Windows.CsWin32
```

Reasons to switch later:

- typed generated bindings
- avoids hand-written fragile P/Invoke
- keeps API usage discoverable from `NativeMethods.txt`
- works well with focused contracts

Current state:

- package not yet installed
- manual COM is active
- `Contracts\NativeMethods.txt` exists as future contract

When package access is available:

```powershell
dotnet add .\Waller.Native.Core\Waller.Native.Core.csproj package Microsoft.Windows.CsWin32
```

Then build Core to generate bindings. Do this only if manual COM becomes
painful or too fragile.

## NativeMethods Contract

Current contract file:

```text
Waller.Native.Core\Contracts\NativeMethods.txt
```

Expected entries over time:

```text
IDesktopWallpaper
GetMonitorInfo
EnumDisplayMonitors
MonitorFromWindow
GetDpiForMonitor
```

Do not add a huge Windows API surface. Add only APIs used by implemented code.

## Monitor Detection

Current implementation:

```text
WindowsMonitorDetector : IMonitorDetector
```

Responsibilities:

- enumerate connected monitors
- get monitor device path/key
- get monitor rectangle in pixels
- get current wallpaper per monitor
- map unsupported/empty state to `WallpaperSource.Empty`
- keep coordinates exactly as Windows reports them

Important:

- negative X/Y are valid
- primary monitor is not guaranteed to be first
- display index is fallback metadata, not stable identity
- dock/GPU changes can alter device IDs

## Wallpaper Read

Use `IDesktopWallpaper` where possible.

Needed capabilities:

- monitor count
- monitor device path
- monitor rectangle
- wallpaper path per monitor

Unknown/empty wallpaper states should become:

```csharp
WallpaperSource.Empty
```

This means black output in Waller's model.

## Wallpaper Apply

Current implementation:

```text
DesktopWallpaperApplier : IWallpaperApplier
```

Input:

- monitor key
- rendered PNG path

Output:

- `ApplyResult`

Rules:

- validate rendered file exists before calling Windows
- apply one monitor at a time
- return structured failure
- never save Presets
- never render images

## Placement Rule

Do not depend on Windows wallpaper position for per-monitor fit.

Waller controls placement by creating final PNG files at each monitor's pixel
size.

This keeps behavior consistent:

```text
Image + Fit + Anchor + Monitor pixels
-> final PNG
-> Windows applies final PNG
```

## Packaged WinUI Launch

Packaged WinUI apps need identity.

Use:

```powershell
powershell -ExecutionPolicy Bypass -File .\BuildAndRun.ps1 .\Waller.Native.App\Waller.Native.App.csproj
```

Do not debug launch failures by double-clicking raw output exe first. That path
can silently fail because package registration/identity is missing.

## Developer Mode

Developer Mode is required for packaged app deploy/run during development.

Check:

```text
Settings -> System -> For developers -> Developer Mode
```

`BuildAndRun.ps1` checks the registry and fails early if Developer Mode is off.

## Common Failure Modes

### App builds but does not open

Likely causes:

- raw exe launched directly
- Developer Mode disabled
- Windows App Runtime mismatch
- package registration failed
- `winapp` unavailable

First action:

```powershell
powershell -ExecutionPolicy Bypass -File .\BuildAndRun.ps1 .\Waller.Native.App\Waller.Native.App.csproj
```

### Monitor list empty

Likely causes:

- detector threw and UI swallowed error
- packaged COM activation issue
- detector used wrong API for package context

Mitigation:

- keep empty monitor fallback available for product runtime
- keep sample detector only for dev/tests
- show friendly detector failure status
- add unit tests for mapping code

### Wrong monitor receives wallpaper

Likely causes:

- monitor key mismatch
- detector and applier using different identifiers
- fallback matching used when exact key existed

Mitigation:

- preserve raw Windows monitor device path
- log or expose debug details in dev-only diagnostics later
- test exact-key path first

### Fit looks wrong

Likely causes:

- renderer used effective UI pixels instead of monitor pixels
- DPI scaling applied incorrectly
- anchor math wrong

Mitigation:

- renderer uses monitor bounds width/height in pixels
- geometry tests for each fit mode

## What Not To Use First

Avoid early:

- broad wrapper libraries for every Windows API
- C++ helper DLL
- Rust helper process
- global Windows wallpaper position as placement solution
- direct exe launch as verification

Add those only if a specific measured problem requires them.
