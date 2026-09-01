# Waller agent notes

Windows-only WinUI 3 wallpaper manager. Product lives in `native/`.

## Load when

- Root commands and local data: [`README.md`](README.md)
- Docs map: [`docs/INDEX.md`](docs/INDEX.md)
- Store submission: [`docs/store/README.md`](docs/store/README.md)
- Contributor workflow: [`CONTRIBUTING.md`](CONTRIBUTING.md)

## Rules

- Dependency direction is `App -> Workflows -> Core`, with `App -> Core` for UI adapters and models.
- `Waller.Native.Core`: domain models, rendering, persistence, Windows contracts
- `Waller.Native.Workflows`: XAML-free product use cases
- `Waller.Native.App`: WinUI composition, projection, pickers, package identity
- Keep Windows-only behavior explicit. Route feature work through public Workflows seams.

## Verification

Native code or build change:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\Invoke-Native.ps1 -Task Verify -SkipSmoke
```

Add surface, Settings, Apply, Release, or Package proof only when the changed risk needs it.

If you run Apply smoke, it temporarily changes the current user's wallpapers and restores them in `finally`.

## Safety

Do not track generated output, certificates, secrets, copied user data, or local absolute paths.
Repository cleanup must not read, migrate, or delete Waller user data.
