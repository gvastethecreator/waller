# Waller Native

WinUI 3 / .NET implementation of Waller.

The repository root owns bootstrap, run, verification, and release entry points. This folder owns the native solution and its operational scripts. Run the commands below from `native/` unless a snippet already includes `native\`.

## Solution layout

```text
Waller.Native.App/        WinUI shell, view models, composition, and Windows adapters
Waller.Native.Workflows/  XAML-free product use cases and shell state
Waller.Native.Core/       domain models, rendering, persistence, and Windows contracts
Waller.Native.Tests/      xUnit tests for Core and public workflows
scripts/                  verification, smoke, packaging, and diagnostics
```

Dependency direction is `App -> Workflows -> Core`, with `App -> Core` for UI adapters. Tests reference Workflows and Core.

## Build

```powershell
dotnet build .\Waller.Native.slnx
```

Build the WinUI packaged app without launching:

```powershell
powershell -ExecutionPolicy Bypass -File .\BuildAndRun.ps1 .\Waller.Native.App\Waller.Native.App.csproj -SkipRun
```

Run tests:

```powershell
dotnet test .\Waller.Native.Tests\Waller.Native.Tests.csproj
```

## Run

Use `BuildAndRun.ps1`. Do not launch the generated `.exe` directly.

```powershell
powershell -ExecutionPolicy Bypass -File .\BuildAndRun.ps1 .\Waller.Native.App\Waller.Native.App.csproj
```

Optional detached run:

```powershell
powershell -ExecutionPolicy Bypass -File .\BuildAndRun.ps1 .\Waller.Native.App\Waller.Native.App.csproj -Detach
```

From the repository root:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\Invoke-Native.ps1 -Task Run
```

## Verify

Full local verification:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\Verify.ps1
```

Fast gate without launching the app:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\Verify.ps1 -SkipSmoke
```

Restricted-network/offline version without NuGet audit warnings:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\Verify.ps1 -SkipSmoke -DisableNuGetAudit
```

Include Release or development MSIX:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\Verify.ps1 -SkipSmoke -Release
powershell -ExecutionPolicy Bypass -File .\scripts\Verify.ps1 -SkipSmoke -Package
```

Apply smoke temporarily changes the current user's wallpapers and restores them in `finally`. See [`docs/TESTING.md`](docs/TESTING.md).

## Runtime notes

Developer Mode must be enabled:

```text
Settings -> System -> For developers -> Developer Mode
```

`BuildAndRun.ps1` includes a fallback lookup for `winapp.exe` in the NuGet cache because `winapp` may not be available on PATH.

If the packaged app builds but does not open, first verify:

- `BuildAndRun.ps1` was used.
- Developer Mode is enabled.
- Windows App Runtime version matches the restored `Microsoft.WindowsAppSDK`.
- The app was launched with package identity, not by double-clicking the raw build output exe.

## App data

Packaged runs store Presets and Settings under:

```text
%LOCALAPPDATA%\Packages\<package-family-name>\LocalCache\Local\Waller
```

Rendered wallpaper PNGs use a shell-readable cache:

```text
%USERPROFILE%\.waller\rendered
```

Original image paths are stored as full local paths. Original files are not copied. Repository cleanup and build commands do not migrate or delete user data.

## Sources and placement

```text
Image       -> original full local image path
SolidColor  -> validated #RRGGBB
Empty       -> black output
```

`Empty` means black wallpaper output. It does not skip the monitor and does not restore the Windows default.

Fit modes: Cover, Contain, Stretch, Center, Tile.

Anchor:

```text
TopLeft    Top     TopRight
Left       Center  Right
BottomLeft Bottom  BottomRight
```

Placement is controlled by prerendering a final PNG at each monitor's pixel size.

## More documentation

- [`docs/TESTING.md`](docs/TESTING.md)
- [`docs/PACKAGING.md`](docs/PACKAGING.md)
- [`docs/WINDOWS_INTEROP.md`](docs/WINDOWS_INTEROP.md)
- [`../docs/store/README.md`](../docs/store/README.md)
