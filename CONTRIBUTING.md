# Contributing to Waller

Waller is a Windows-only WinUI 3 application. Keep changes aligned with the native product and the terms in [`CONTEXT.md`](./CONTEXT.md).

## Before you start

- Read [`README.md`](./README.md), [`CONTEXT.md`](./CONTEXT.md), [`docs/INDEX.md`](./docs/INDEX.md), and [`native/docs/ARCHITECTURE.md`](./native/docs/ARCHITECTURE.md).
- Preserve the dependency direction `Waller.Native.App -> Waller.Native.Core`.
- Keep domain and workflow behavior in Core when it does not require WinUI or package identity.
- Keep Windows and WinUI adapters in App.
- Do not add cross-platform abstractions to this Windows-only product without a concrete requirement.

## Local setup

Install the pinned SDK into the workspace if the host does not already provide it:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\BootstrapDotnet.ps1
```

Run the packaged app:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\Invoke-Native.ps1 -Task Run
```

## Verification

Use the narrowest check that can falsify the change while developing. Before handing off a native code or build change, run:

```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\Invoke-Native.ps1 -Task Verify -SkipSmoke
```

Add packaged surface, Settings roundtrip, or Apply smoke only when the changed risk needs it. Apply smoke temporarily changes and then restores the current user's wallpapers.

## Pull request checklist

- [ ] The change uses Monitor, Active Session, Preset, Wallpaper Source, Monitor Assignment, Save, and Apply consistently.
- [ ] App/Core boundaries remain explicit and callers use public seams.
- [ ] A behavior change extends the nearest existing test or guard when that adds distinct evidence.
- [ ] The relevant root and native documentation remains accurate.
- [ ] `Invoke-Native.ps1 -Task Verify -SkipSmoke` passes.
- [ ] Packaging-sensitive changes also pass a Release build or development MSIX check.
- [ ] No generated output, certificate, secret, user data, or local absolute path is tracked.

## Reporting issues

Use [GitHub Issues](https://github.com/gvastethecreator/waller/issues) for normal defects. Include the package version from `native/Waller.Native.App/Package.appxmanifest`, Windows build and architecture, reproducible steps, expected behavior, and actual behavior.

Report vulnerabilities privately as described in [`SECURITY.md`](./SECURITY.md).
