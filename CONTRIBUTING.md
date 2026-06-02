# Contributing to Waller

Thanks for your interest in Waller. This project follows a small set of conventions so
contributions stay easy to review and align with the **Wallpaper Session** seam.

## Before you start

- Read [`README.md`](./README.md), [`docs/INDEX.md`](./docs/INDEX.md), and
  [`src/CONTEXT.md`](./src/CONTEXT.md) first.
- Use the project vocabulary: **Monitor**, **Wallpaper Source**, **Wallpaper Draft**,
  **Wallpaper Session**, **Profile**, **Preview**, and **Identify Overlay**.
- Waller is Windows-only by design. Do not introduce fake cross-platform abstractions
  unless a task explicitly requires them.

## Local setup

Requirements:

- Windows 10/11
- [Bun](https://bun.sh/) `1.3.x`
- Stable Rust toolchain (`x86_64-pc-windows-msvc`)
- Microsoft Visual C++ Build Tools if your environment is not already set up for
  native crates

```sh
bun install
bun run dev
```

## Development loop

For code changes:

```sh
bun run verify
```

For dependency changes, run the alignment check first and then verify:

```sh
bun run deps:tauri:check
bun run verify
```

For packaging or release-sensitive work, also run:

```sh
bun run build
```

Useful entry points:

- `src/hooks/useWallpaperSession.ts` and `src/lib/wallpaperSession.ts` — frontend seam
- `src/lib/tauri.ts` — typed Tauri IPC adapter
- `src-tauri/src/lib.rs` — backend command boundary
- `src-tauri/src/wallpaper.rs` and `src-tauri/src/wallpaper_value.rs` — native logic
- `src-tauri/src/profiles.rs` — local profile persistence

## Pull request checklist

- [ ] Tests cover the change. Add or update the closest seam-level test
      (`src/lib/*.test.ts` or `src-tauri/src/*`).
- [ ] Both sides of the JS/Rust boundary stay aligned for any change in
      Wallpaper Source, fit mode, Profile, Preview, or Identify Overlay semantics.
- [ ] `bun run verify` passes locally.
- [ ] `README.md`, `docs/INDEX.md`, and the relevant `docs/*.md` files are still
      accurate.
- [ ] No new tracked build output, editor-specific config, secrets, or local
      absolute paths.

## Reporting issues

Use [GitHub Issues](https://github.com/gvastethecreator/waller/issues). Include:

- Waller version (from `src-tauri/tauri.conf.json`)
- Windows build number
- Reproducible steps and expected vs. actual behavior
- Relevant log lines from the **View Logs** modal or
  `%APPDATA%/WallpaperManager/logs/app.log`

## Code of conduct

Be respectful. This is a small, focused desktop tool — we value clarity,
traceability, and reversible decisions over speed.
