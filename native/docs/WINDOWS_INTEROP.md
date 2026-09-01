# Windows interop notes

Waller talks to Windows through small Core contracts:

```text
IMonitorDetector
IWallpaperApplier
IWallpaperRenderer
```

The WinUI app should not call `IDesktopWallpaper`, P/Invoke, or COM directly.

## Current path

Current code uses focused manual COM interop for `IDesktopWallpaper`.

Manual COM activation uses `Type.GetTypeFromCLSID` plus `Activator`. The interop factory has an explicit trim-analysis suppression because the COM class is not constructed through a managed public parameterless constructor. Do not remove that suppression unless the activation path moves to generated interop.

Implemented:

- `DesktopWallpaperInterop`
- `WindowsMonitorDetector`
- `DesktopWallpaperApplier`

The contract file is `Waller.Native.Core\Contracts\NativeMethods.txt`. Add only APIs used by implemented code.

## Monitor detection

`WindowsMonitorDetector` enumerates connected monitors, device path/key, pixel rectangle, and current wallpaper. Unsupported or empty wallpaper states map to `WallpaperSource.Empty`. Coordinates stay exactly as Windows reports them.

- Negative X/Y are valid.
- The primary monitor is not guaranteed to be first.
- Display index is fallback metadata, not stable identity.
- Dock/GPU changes can alter device IDs.

## Wallpaper apply

`DesktopWallpaperApplier` takes a monitor key and a rendered PNG path and returns `ApplyResult`.

- Validate the rendered file exists before calling Windows.
- Apply one monitor at a time.
- Return structured failure.
- Never save Presets.
- Never render images.

## Placement

Do not depend on Windows wallpaper position for per-monitor fit. Waller prerenders a final PNG at each monitor's pixel size, then Windows applies that PNG.

## Packaged launch

Packaged WinUI apps need identity. From `native/`:

```powershell
powershell -ExecutionPolicy Bypass -File .\BuildAndRun.ps1 .\Waller.Native.App\Waller.Native.App.csproj
```

Do not debug launch failures by double-clicking the raw output exe. That path can fail silently because package registration is missing.

Developer Mode is required:

```text
Settings -> System -> For developers -> Developer Mode
```

`BuildAndRun.ps1` checks the registry and fails early if Developer Mode is off.

## Common failure modes

### App builds but does not open

Likely causes: raw exe launched directly, Developer Mode disabled, Windows App Runtime mismatch, package registration failed, or `winapp` unavailable.

### Monitor list empty

Likely causes: detector threw and UI swallowed the error, packaged COM activation issue, or the detector used the wrong API for package context. Keep the empty-monitor fallback for product runtime. Keep the sample detector only for tests.

### Wrong monitor receives wallpaper

Likely causes: monitor key mismatch, detector and applier using different identifiers, or fallback matching used when an exact key existed. Preserve the raw Windows monitor device path and test the exact-key path first.

### Fit looks wrong

Likely causes: renderer used effective UI pixels instead of monitor pixels, DPI scaling applied incorrectly, or anchor math is wrong. The renderer must use monitor bounds width/height in pixels.
