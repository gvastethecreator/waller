# Security

## Reporting a vulnerability

Do not open a public issue for a security problem. Use [GitHub Security Advisories](https://github.com/gvastethecreator/waller/security/advisories/new) and include:

- Waller package version from `native/Waller.Native.App/Package.appxmanifest`
- Windows build and architecture
- reproduction steps and impact
- a minimal proof of concept when available

## Current boundary

Waller is a packaged WinUI 3 desktop application with full-trust access required for Windows wallpaper integration.

- The app reads monitor data and applies wallpapers through `IDesktopWallpaper`.
- It reads only image paths selected by the user or stored in a local Preset.
- Presets and settings remain in package-local app data.
- Rendered PNGs are written to `%USERPROFILE%\.waller\rendered` because the Windows shell must read them.
- The current product has no account, telemetry, updater, remote service, embedded web runtime, or application network client.
- JSON input, paths, enum values, package identity, and workflow status are validated at native boundaries.
- Apply smoke restores the previous wallpapers in `finally`; ordinary verification does not alter user data.

Development certificates and generated packages are ignored. Never commit `.pfx`, `.cer`, MSIX output, production signing material, secrets, or copied user data.

## Changes that require security review

Review and document changes that add network access, new package capabilities, shell execution, new file-system roots, untrusted data parsing, update/install behavior, or production signing. Run the native verification gate and the smallest relevant packaged smoke before release handoff.
