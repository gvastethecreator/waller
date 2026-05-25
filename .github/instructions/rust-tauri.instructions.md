---
description: "Use when editing Rust backend code, Tauri commands, Win32 wallpaper integration, Cargo manifests, or Tauri configuration. Covers validation, blocking work, and JS/Rust version sync."
name: "Rust Tauri Backend"
applyTo:
  - "src-tauri/**/*.rs"
  - "src-tauri/Cargo.toml"
  - "src-tauri/tauri.conf.json"
---
# Rust/Tauri backend rules

- Keep blocking filesystem, COM, and Win32 work behind `run_blocking`/`spawn_blocking` in `src-tauri/src/lib.rs`.
- Validate inputs at the command boundary and return typed `AppError` / `CommandError` values instead of panicking.
- Keep `src-tauri/src/wallpaper_value.rs` aligned with frontend modules such as `wallpaperSource.ts` and `profileComposition.ts` when markers, fit modes, or limits change.
- Keep `@tauri-apps/*` JavaScript packages and Cargo `tauri*` crates aligned; run `bun run deps:tauri:check` after manifest edits.
- Preserve the Windows-only focus and explicit `windows` crate feature declarations.
