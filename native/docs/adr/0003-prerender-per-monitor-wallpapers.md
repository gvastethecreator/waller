# ADR 0003: Prerender Per-Monitor Wallpapers

Date: 2026-06-04

## Status

Accepted.

## Context

Product requires per-monitor fit and position:

- Cover
- Contain
- Stretch
- Center
- Tile
- 3x3 anchor

Windows wallpaper placement settings are global or inconsistent for per-monitor
custom placement.

## Decision

Render final PNG files per monitor before applying them to Windows.

Pipeline:

```text
Source + Placement + Monitor pixels
-> render final PNG
-> apply rendered PNG to that monitor
```

## Consequences

Positive:

- predictable per-monitor behavior
- app controls Cover/Contain/Anchor math
- Windows receives simple final files
- easier to test placement math

Negative:

- needs renderer implementation
- needs app-managed rendered cache
- large images can use memory/time

## Follow-Up

Implement renderer after monitor detection. Add geometry tests before wiring real
apply.

