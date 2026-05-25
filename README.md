# Waller — Multi-Monitor Wallpaper Manager (Windows)

Waller is a **Windows 10/11** desktop application built with **Tauri 2, Rust, React 19, TypeScript 6, Vite 8, Tailwind 4, and Bun**. It manages one **Wallpaper Session** per app run, supports reusable **Profiles**, resolves **Previews** on demand, includes a lightweight image editor, and keeps a persistent diagnostic trail through backend logs.

## What it does

- Detects active monitors through `IDesktopWallpaper`, with GDI geometry support for layout and fallback visualization.
- Lets each **Monitor** use one **Wallpaper Source**:
  - Local image
  - Solid colour marker (`__SOLID__:#RRGGBB`)
  - No wallpaper marker (`__NONE__`)
- Supports fit modes: `Center`, `Tile`, `Stretch`, `Fit`, `Fill`, and `Span`.
- Saves, loads, lists, and deletes JSON **Profiles** in `%APPDATA%/WallpaperManager/profiles`.
- Generates and caches image **Previews** through a dedicated registry.
- Provides an **Identify Overlay** to map physical screens to display indices.
- Includes a lightweight non-destructive editor for pan/zoom/rotate/filter/tint adjustments before saving a PNG.
- Stores unified frontend/backend logs at `%APPDATA%/WallpaperManager/logs/app.log`.
- Ships with English and Spanish UI strings.

## Architecture at a glance

- `src/App.tsx` composes the shell, modal flows, layout overview, and toasts.
- `src/hooks/useWallpaperSession.ts` is the React seam that exposes grouped actions (`session`, `monitorDrafts`, `profiles`, `editor`, `previews`).
- `src/lib/wallpaperSession.ts` owns the command queue, snapshot building, preview warming, editor state, and identify fallback logic.
- `src/lib/profileComposition.ts`, `previewRegistry.ts`, `wallpaperSessionState.ts`, and `wallpaperSource.ts` keep the pure frontend domain logic focused.
- `src/lib/tauri.ts` is the typed frontend IPC seam over Tauri commands and plugins.
- `src-tauri/src/lib.rs` exposes the backend commands and serializes blocking wallpaper work through `run_blocking`.
- `src-tauri/src/wallpaper.rs`, `profiles.rs`, `logger.rs`, and `wallpaper_value.rs` implement native wallpaper control, profile persistence, logging, and shared value validation.

## Current stack snapshot

### Web/runtime packages

- `react` / `react-dom` `19.2.6`
- `typescript` `6.0.3`
- `vite` `8.0.14`
- `@vitejs/plugin-react` `6.0.2`
- `tailwindcss` / `@tailwindcss/vite` `4.3.0`
- `vitest` / `@vitest/coverage-istanbul` `4.1.7`
- `jsdom` `29.1.1`
- `oxlint` `1.66.0`
- `@tauri-apps/api` `2.11.0`
- `@tauri-apps/plugin-dialog` `2.7.1`
- `@tauri-apps/plugin-log` `2.8.0`
- `@tauri-apps/cli` `2.11.2`

### Rust/Tauri packages

- `tauri` `2.11.2`
- `tauri-build` `2.6.2`
- `tauri-plugin-dialog` `2.7.1`
- `tauri-plugin-log` `2.8.0`
- `windows` `0.62.2`

## Requirements

- Windows 10/11
- [Bun](https://bun.sh/) `1.3.x`
- Stable Rust toolchain (MSVC target)
- Microsoft Visual C++ Build Tools if the environment is not already configured to build native crates

## Getting started

```sh
bun install
bun run dev
```

Development uses `http://localhost:3000` for the Vite frontend, which Tauri consumes in dev mode.

## Useful scripts

| Script | Description |
|---|---|
| `bun run dev` | Full Tauri + Vite development flow |
| `bun run web:dev` | Frontend-only Vite server |
| `bun run web:build` | Frontend production build |
| `bun run typecheck` | Strict TypeScript validation |
| `bun run lint:frontend` | `oxlint` with warnings denied |
| `bun run test:frontend` | Vitest + coverage |
| `bun run test:rust` | `cargo test --lib` in `src-tauri` |
| `bun run lint:backend` | `cargo clippy -- -D warnings` |
| `bun run check:rust` | `cargo check` |
| `bun run deps:web:check` | Inspect outdated web dependencies |
| `bun run deps:web:update` | Refresh web dependencies to latest |
| `bun run deps:rust:update` | Refresh Cargo lockfile |
| `bun run deps:tauri:check` | Verify JS/Rust Tauri version alignment |
| `bun run deps:update` | Run the broad dependency maintenance flow |
| `bun run verify` | Full repo verification, including Tauri version sync |
| `bun run build` | Tauri production build |

## Verification and release

For meaningful code or dependency changes, run:

```sh
bun run verify
```

For packaging-sensitive work, add:

```sh
bun run build
```

CI and release workflows live in `.github/workflows/` and run on `windows-latest`.

## Shared project rules and skills

This repository now includes shared agent/project guidance in:

- `.github/copilot-instructions.md`
- `.github/instructions/`
- `.github/skills/waller-maintenance/`
- `.github/skills/waller-wallpaper-session/`

These files encode the repo vocabulary, seam boundaries, maintenance workflow, and verification expectations.

## Security and observability

- Hardened CSP in `src-tauri/tauri.conf.json`
- `withGlobalTauri = false`
- Minimal capabilities in `src-tauri/capabilities/default.json`
- Unified logging with `tauri-plugin-log`
- Log rotation at `2 MiB` with `app.log.bak`
- Explicit Tauri JS/Rust version-alignment check in `scripts/check-tauri-version-sync.mjs`

## Documentation map

- `docs/INDEX.md` — entry point for architecture, implementation, testing, UI, roadmap, and maintenance notes
- `src/CONTEXT.md` — domain vocabulary used across the codebase

## License

MIT
