---
description: "Use when updating Waller documentation, workflows, package behavior, or project rules."
name: "Documentation Sync"
applyTo:
  - "AGENTS.md"
  - "README.md"
  - "docs/**/*.md"
  - "native/**/*.md"
  - ".github/**/*.md"
---
# Documentation sync rules

Follow [`AGENTS.md`](../../AGENTS.md). Path-specific extras:

- Update documentation in the same change when scripts, workflows, release steps, local-data behavior, or visible product behavior changes.
- Keep `docs/INDEX.md` free of missing or obsolete links.
- Use commands that run from the repository root.
- Treat `native/` as the definitive product path.
- Mark historical implementation comparisons explicitly; do not present retired technology as active.
