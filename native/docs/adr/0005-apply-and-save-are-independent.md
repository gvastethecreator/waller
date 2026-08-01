# ADR 0005: Apply And Save Are Independent

Date: 2026-06-04

## Status

Accepted.

## Context

Wallpaper apps often blur "save config" and "apply to desktop". Waller Native
needs both without surprise behavior.

## Decision

Keep Apply and Save separate.

Apply:

- renders final wallpaper files
- changes Windows
- does not save Preset

Save:

- persists current Active Session as Preset
- does not change Windows

## Consequences

Positive:

- no accidental Windows changes when saving
- no accidental Preset overwrite when applying
- user can test wallpaper without committing Preset
- user can prepare Preset without changing desktop

Negative:

- UI must clearly show Applied vs Saved state
- dirty state needs careful wording

## Follow-Up

Use row/header status to distinguish:

```text
Applied / Pending / Error
Saved / Unsaved
```

