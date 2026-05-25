---
name: waller-wallpaper-session
description: 'Extend or debug the Wallpaper Session, monitor drafts, preview flows, editor flow, and profile composition across React, Tauri, and Rust. Use for monitor wallpaper features, profile bugs, preview issues, or domain-model changes.'
argument-hint: 'Describe the Wallpaper Session or monitor/profile flow you want to change.'
---

# Waller Wallpaper Session workflow

## When to use

- Changing monitor draft behavior or fit-mode handling
- Fixing preview loading, caching, or editor save/apply flows
- Updating profile composition or profile validation rules
- Coordinating a domain change across TypeScript and Rust markers/IPC

## Files to inspect first

- `src/CONTEXT.md`
- `src/hooks/useWallpaperSession.ts`
- `src/lib/wallpaperSession.ts`
- `src/lib/wallpaperSessionState.ts`
- `src/lib/profileComposition.ts`
- `src/lib/previewRegistry.ts`
- `src/lib/wallpaperSource.ts`
- `src/lib/tauri.ts`
- `src-tauri/src/lib.rs`
- `src-tauri/src/wallpaper.rs`
- `src-tauri/src/wallpaper_value.rs`
- `src-tauri/src/profiles.rs`

## Procedure

1. Identify whether the change belongs to pure frontend state, the Tauri IPC seam, or the Rust/native layer.
2. Preserve the domain terms from `src/CONTEXT.md` in new symbols and tests.
3. Keep presentational UI in components and orchestration in `useWallpaperSession` / `wallpaperSession`.
4. If a marker, fit mode, profile limit, or persisted shape changes, update both TypeScript and Rust validators in the same task.
5. Add or update tests close to the seam you changed (`src/lib/*.test.ts` and `src-tauri/src/*` tests).
6. Run `bun run verify` before finishing.

## Common traps

- Changing profile validation only on one side of the JS/Rust boundary
- Bypassing `src/lib/tauri.ts` from components
- Forgetting to update docs when session flows or commands change
- Treating Preview state as UI-only when it is part of the Wallpaper Session contract
