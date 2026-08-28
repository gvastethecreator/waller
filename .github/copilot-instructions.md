# Waller project guidelines

## Product language

- Use the canonical terms in `CONTEXT.md`: Monitor, Active Session, Current Setup, Wallpaper Source, Monitor Assignment, Placement, Rendered Wallpaper, Preset, Save, Apply, and Preview.
- Do not reintroduce the retired Wallpaper Session/Profile vocabulary into active code or documentation.

## Architecture

- `native/Waller.Native.Core` owns domain models, rendering, persistence contracts, and Windows-facing policy.
- `native/Waller.Native.Workflows` owns XAML-free multi-step product use cases (Preset, Apply, monitor editing, settings, shell).
- `native/Waller.Native.App` owns WinUI composition, UI projection, package identity, pickers, and Windows adapters.
- Keep dependency direction `App -> Workflows -> Core`, with `App -> Core` for adapters and models; Core and Workflows must not reference App, XAML, or WinUI.
- Keep Windows-only behavior explicit. Do not add fake cross-platform abstractions.
- Route feature work through public Workflows seams instead of growing `MainPageViewModel` orchestration.

## Verification

- Run `.\scripts\Invoke-Native.ps1 -Task Verify -SkipSmoke` for native code and build changes.
- Add surface, Settings, Apply, Release, or Package proof only when the changed risk needs it.
- Never run Apply smoke without acknowledging that it temporarily changes the current user's wallpapers and restores them in `finally`.

## Documentation and safety

- Keep `README.md`, `CONTEXT.md`, `docs/INDEX.md`, and the relevant native docs aligned.
- Do not track generated output, certificates, secrets, copied user data, or local absolute paths.
- Repository cleanup must not read, migrate, or delete Waller user data.
