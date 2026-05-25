---
name: waller-maintenance
description: 'Refresh dependencies, align Tauri JS/Rust versions, sync documentation, and run the full maintenance verification flow. Use for repo maintenance, dependency upgrades, release prep, CI/doc sync, or technical housekeeping.'
argument-hint: 'Describe the maintenance goal, such as update deps, sync docs, or prepare a release.'
---

# Waller maintenance workflow

## When to use

- Updating web or Rust dependencies
- Refreshing lockfiles or verifying Tauri package alignment
- Syncing README / docs after architectural or workflow changes
- Preparing CI-safe maintenance or release work

## Procedure

1. Read `package.json`, `src-tauri/Cargo.toml`, `README.md`, `docs/INDEX.md`, `docs/03-SERVICES-AND-DEPENDENCIES.md`, `docs/06-TESTING-AND-OPERATIONS.md`, and relevant workflows.
2. Update web dependencies from the repo root with `bun run deps:web:update` when a broad refresh is required.
3. Update Rust dependencies in `src-tauri/Cargo.toml` as needed, then refresh the lockfile with `bun run deps:rust:update`.
4. Run `bun run deps:tauri:check` to confirm JS and Rust Tauri/plugin versions still line up.
5. Fix any compile/test breakage before touching release workflows.
6. Update README and the relevant files under `docs/` so the repo describes the current versions, scripts, seams, and operational steps.
7. Finish with `bun run verify`; add `bun run build` when packaging or installer output is part of the change.

## Project-specific reminders

- This is a Windows-only Tauri desktop app.
- `src/CONTEXT.md` defines the preferred domain vocabulary.
- `docs/10-MAINTENANCE-AND-STACK-UPDATE-2026-05-25.md` is the running maintenance snapshot for the current stack shape.
