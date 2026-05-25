# Services, APIs & Dependencies

## Stack snapshot (2026-05-25)

### Web dependencies (`package.json`)

| Package | Version | Role |
|---|---:|---|
| `react` | `19.2.6` | Main UI runtime |
| `react-dom` | `19.2.6` | DOM renderer |
| `typescript` | `6.0.3` | Static typing |
| `vite` | `8.0.14` | Dev server and bundler |
| `@vitejs/plugin-react` | `6.0.2` | React/Vite integration |
| `tailwindcss` | `4.3.0` | Utility-first styling |
| `@tailwindcss/vite` | `4.3.0` | Tailwind/Vite integration |
| `gsap` | `3.15.0` | UI motion and list/overlay animation |
| `@gsap/react` | `2.1.2` | React integration for GSAP |
| `vitest` | `4.1.7` | Frontend/unit test runner |
| `@vitest/coverage-istanbul` | `4.1.7` | Coverage provider |
| `@testing-library/react` | `16.3.2` | DOM/component testing |
| `@testing-library/jest-dom` | `6.9.1` | DOM assertions |
| `@testing-library/user-event` | `14.6.1` | Higher-level UI interactions |
| `jsdom` | `29.1.1` | Browser-like test environment |
| `oxlint` | `1.66.0` | Frontend linting |
| `@tauri-apps/api` | `2.11.0` | Frontend Tauri API access |
| `@tauri-apps/plugin-dialog` | `2.7.1` | Native file picker |
| `@tauri-apps/plugin-log` | `2.8.0` | Frontend log bridge |
| `@tauri-apps/cli` | `2.11.2` | Dev/build CLI |

### Rust dependencies (`src-tauri/Cargo.toml`)

| Crate | Version | Role |
|---|---:|---|
| `tauri` | `2.11.2` | Main runtime |
| `tauri-build` | `2.6.2` | Build-time codegen/config support |
| `tauri-plugin-dialog` | `2.7.1` | Native dialog support |
| `tauri-plugin-log` | `2.8.0` | Unified runtime logging |
| `serde` | `1.0.228` | Serialization |
| `serde_json` | `1.0.150` | JSON persistence |
| `dirs` | `6.0.0` | AppData path discovery |
| `base64` | `0.22.1` | Data URL preview payloads |
| `thiserror` | `2.0.18` | Typed backend errors |
| `log` | `0.4.29` | Runtime log facade |
| `windows` | `0.62.2` | Win32 / COM bindings |

## Tauri version-alignment policy

The repo now enforces explicit JS/Rust alignment through:

- `scripts/check-tauri-version-sync.mjs`
- `bun run deps:tauri:check`
- `bun run verify`

Alignment rules:

- `@tauri-apps/api`, `@tauri-apps/cli`, and Cargo `tauri` must stay on the same `major.minor` line.
- JS/Rust plugin pairs such as `plugin-dialog` and `plugin-log` must match exactly.
- `tauri-build` is reported by the script for visibility, but not line-enforced because its published version line does not currently track Cargo `tauri` one-to-one.

## Windows APIs used

### `IDesktopWallpaper`

- monitor enumeration
- `SetWallpaper`
- `GetWallpaper`
- `SetPosition`
- `GetPosition`

### GDI support

- `EnumDisplayMonitors`
- `GetMonitorInfoW`

GDI is used for geometry/visualization fallback and monitor layout support when COM data is incomplete.

## Internal services

| Service | Files | Responsibility |
|---|---|---|
| Wallpaper Session | `src/hooks/useWallpaperSession.ts`, `src/lib/wallpaperSession.ts` | Main frontend session orchestration |
| Draft state | `src/lib/wallpaperSessionState.ts` | Draft/baseline transitions and dirty tracking |
| Profile composition | `src/lib/profileComposition.ts` | Save/load validation and projection |
| Preview registry | `src/lib/previewRegistry.ts` | Deduplicated preview state |
| Wallpaper source semantics | `src/lib/wallpaperSource.ts`, `src-tauri/src/wallpaper_value.rs` | Marker parsing/encoding and fit handling |
| Tauri IPC adapter | `src/lib/tauri.ts` | Typed invoke/plugin wrapper layer |
| Backend commands | `src-tauri/src/lib.rs` | Runtime command boundary |
| Native wallpaper control | `src-tauri/src/wallpaper.rs` | COM/GDI monitor + wallpaper integration |
| Profiles | `src-tauri/src/profiles.rs` | JSON profile persistence and validation |
| Logging | `src-tauri/src/logger.rs` | Persistent logging, read/clear, rotation |

## Operational limits and contracts

These values are enforced in code and should stay aligned across docs and both sides of the JS/Rust seam:

| Contract | Value | Source |
|---|---:|---|
| Profile name max length | `80` chars | `profileComposition.ts`, `profiles.rs` |
| Profile max monitor entries | `32` | `profileComposition.ts`, `profiles.rs` |
| Profile image path max length | `4096` chars | `profileComposition.ts`, `profiles.rs` |
| Preview file max size | `20 MiB` | `src-tauri/src/lib.rs` |
| Edited PNG max size | `50 MiB` | `src-tauri/src/lib.rs` |
| Identify window auto-close delay | `1800 ms` | `src-tauri/src/lib.rs` |
| Log rotation threshold | `2 MiB` | `logger.rs` |

## Tauri capabilities

File: `src-tauri/capabilities/default.json`

Relevant permissions:

- `core:default`
- `dialog:default`
- `dialog:allow-open`
- `core:window:default`
- `core:window:allow-set-title`
- `log:default`

## Security considerations

- `withGlobalTauri` stays disabled.
- CSP only allows local resources, `data:` payloads needed by previews/fonts, and the local dev server endpoints.
- IPC commands validate inputs and return serialized, typed errors.
- The project intentionally remains Windows-only; there are no fake portability layers around Win32 wallpaper behavior.
