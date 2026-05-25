# Project Guidelines

## Domain language

- Use the vocabulary in `src/CONTEXT.md`: **Monitor**, **Wallpaper Source**, **Wallpaper Draft**, **Wallpaper Session**, **Profile**, **Preview**, and **Identify Overlay**.
- Prefer those terms in code, tests, and documentation instead of generic alternatives like `screen`, `config`, or `thumbnail`.

## Architecture

- Frontend orchestration should flow through `src/hooks/useWallpaperSession.ts` and `src/lib/wallpaperSession.ts`.
- Keep pure frontend rules in focused modules such as `profileComposition`, `previewRegistry`, `wallpaperSessionState`, and `wallpaperSource`.
- Keep Tauri IPC concentrated in `src/lib/tauri.ts`; avoid calling Tauri APIs directly from components.
- Rust/Tauri commands live in `src-tauri/src/lib.rs`; blocking filesystem or Win32 work must remain behind `run_blocking`/`spawn_blocking`.
- When changing wallpaper markers, fit modes, or profile limits, keep the TypeScript and Rust validation layers aligned.

## Verification

- Run `bun run verify` for code changes from the repository root.
- For dependency changes, run `bun run deps:tauri:check` and then `bun run verify`.
- For packaging or release-sensitive work, also run `bun run build`.

## Documentation

- Treat documentation as part of the deliverable.
- Update `README.md` and the relevant files in `docs/` whenever architecture, scripts, dependencies, workflows, or user-visible flows change.
- Keep `docs/INDEX.md` free of stale or missing links.

## Platform conventions

- This project is Windows-only today; do not introduce fake cross-platform abstractions unless the task explicitly requires them.
- Keep security posture conservative: minimal Tauri capabilities, no global Tauri injection, validated inputs at IPC boundaries.
