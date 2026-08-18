# Waller

Waller is a native Windows wallpaper manager for multi-monitor setups. The definitive product is the WinUI 3 application under `native/`, built with C# and .NET 10.

It keeps one editable **Active Session**, lets every **Monitor** use its own **Wallpaper Source** and placement, saves reusable local **Presets**, and applies rendered wallpapers through Windows `IDesktopWallpaper`.

## Current product

- Detects connected monitors and their current wallpapers.
- Supports local images, solid colors, and empty assignments.
- Supports Cover, Contain, Stretch, Center, and Tile placement.
- Saves, loads, renames, duplicates, and deletes local Presets.
- Renders one shell-readable PNG per monitor before Apply.
- Reports Apply progress and supports cancellation.
- Provides English and Spanish UI, light/dark themes, keyboard access, and packaged launch diagnostics.
- Keeps Save and Apply independent: Save persists a Preset; Apply changes Windows.

The removed web/Tauri implementation remains available through Git history only.

## Solution map

```text
native/
  Waller.Native.App/    WinUI shell, view models, composition, and Windows adapters
  Waller.Native.Workflows/ XAML-free use cases and shell state
  Waller.Native.Core/   domain models, rendering, persistence, and Windows contracts
  Waller.Native.Tests/  xUnit tests for Core and public workflows
  scripts/              verification, smoke, packaging, and diagnostic commands
```

Dependency direction is `App -> Workflows -> Core`, with `App -> Core` for UI adapters. Tests reference Workflows and Core. Active domain language lives in [`CONTEXT.md`](./CONTEXT.md).

## Requirements

- Windows 10 version 1809 or newer
- .NET SDK `10.0.302` or a compatible patch selected by [`global.json`](./global.json)
- Developer Mode for packaged local launch

The repository can install the required SDK into `.scratch/toolchains` without changing the machine-wide installation:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\BootstrapDotnet.ps1
```

## Run

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\Invoke-Native.ps1 -Task Run
```

The root executor uses the workspace-local SDK when present and delegates to the canonical native scripts.

## Verify

Fast repository gate without launching the app:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\Invoke-Native.ps1 -Task Verify -SkipSmoke
```

Packaged launch and optional UI Automation smoke:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\Invoke-Native.ps1 -Task Verify -SurfaceSmoke -SettingsRoundTrip
```

Apply smoke temporarily changes the current user's wallpapers and restores them in `finally`. Run it only when that desktop mutation is acceptable:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\Invoke-Native.ps1 -Task Verify -ApplySmoke
```

## Build and packaging

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\Invoke-Native.ps1 -Task Release
powershell -ExecutionPolicy Bypass -File .\scripts\Invoke-Native.ps1 -Task Package
```

`Package` creates a development-signed MSIX under `native/artifacts/`. It is for local install validation, not public distribution. Production signing, Store publication, and clean-machine qualification remain separate release gates.

Microsoft Store preparation, Partner Center identity setup, `.msixupload` generation, certification notes, listing copy, and lifecycle evidence are documented in [`docs/store/README.md`](./docs/store/README.md). The Store build remains blocked until Waller has a reserved Partner Center identity.

## Local data

- Presets and settings use package-local `LocalCache\Local\Waller` storage.
- Rendered wallpapers use `%USERPROFILE%\.waller\rendered` so the Windows shell can read them.
- Repository cleanup and build commands do not migrate or delete user data.

See [`PRIVACY.md`](./PRIVACY.md) for the public data-access and retention policy.

## Documentation

- [`docs/INDEX.md`](./docs/INDEX.md) — active documentation map
- [`native/docs/README.md`](./native/docs/README.md) — native architecture and operations
- [`docs/store/README.md`](./docs/store/README.md) — Microsoft Store submission runbook
- [`PRIVACY.md`](./PRIVACY.md) — bilingual privacy policy
- [`docs/architecture/winui-definitive-architecture-spec.md`](./docs/architecture/winui-definitive-architecture-spec.md) — approved architecture specification
- [`docs/architecture/WORKPLAN.md`](./docs/architecture/WORKPLAN.md) — architecture execution tracker
- [`docs/architecture/winui-definitive-architecture-report.md`](./docs/architecture/winui-definitive-architecture-report.md) — completed ten-ticket architecture report

## License

MIT
