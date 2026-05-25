---
description: "Use when updating README, PRDs, docs, maintenance notes, agent rules, or project workflows. Covers documentation sync, index hygiene, and release/verification guidance."
name: "Documentation Sync"
applyTo:
  - "README.md"
  - "PRD.md"
  - "docs/**/*.md"
  - ".github/**/*.md"
---
# Documentation sync rules

- Update docs in the same task when changing dependencies, architecture, scripts, workflows, release steps, or visible product behavior.
- Keep `docs/INDEX.md` accurate; never leave dead links or references to missing files.
- Name the real seams in architecture docs: `useWallpaperSession`, `wallpaperSession`, `profileComposition`, `previewRegistry`, `wallpaperSource`, and `wallpaper_value.rs`.
- Prefer commands that run from the repository root (`bun run ...`) and call out Windows-specific assumptions clearly.
- When adding shared agent rules or skills, mention them in the maintenance/update documentation so contributors can discover them.
