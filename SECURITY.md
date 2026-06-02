# Security

## Reporting a vulnerability

Please do **not** open a public issue for security problems.

Use [GitHub Security Advisories](https://github.com/gvastethecreator/waller/security/advisories/new)
to report a vulnerability privately. Include:

- Waller version (from `src-tauri/tauri.conf.json`)
- Windows build and architecture
- Reproduction steps and impact
- A minimal proof of concept if available

You can expect an acknowledgement within a reasonable timeframe. We will coordinate
a fix and a release before any public disclosure.

## Security posture of the current release

Waller is a Windows desktop application that uses Tauri 2, a Rust backend, and a
React/TypeScript frontend rendered inside the system WebView. The current
posture includes:

- `withGlobalTauri = false` in `src-tauri/tauri.conf.json`
- A minimal capability set in `src-tauri/capabilities/default.json`
- A strict Content Security Policy that only allows local resources, the IPC
  endpoints, and the local Vite dev server during development
- All Tauri IPC commands validate their inputs and return typed
  `CommandError` payloads
- Blocking native work is isolated behind `run_blocking` / `spawn_blocking`
- No telemetry, no auto-update, no network calls outside the local dev server

Waller reads user-selected image paths and the monitor inventory reported by
`IDesktopWallpaper`. It does not exfiltrate any other system data.

## What to update if you change the security boundary

- Edit `src-tauri/capabilities/default.json` only when a feature genuinely
  requires new permissions. Keep the capability list as small as possible.
- Edit the CSP in `src-tauri/tauri.conf.json` when a new origin or `data:`
  source must be reachable from the WebView.
- Re-run `bun run verify` and a manual `bun run build` after any change to
  capabilities, the CSP, or the Tauri configuration.
