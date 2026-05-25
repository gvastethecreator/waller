# Waller

This context covers preparing, previewing, editing, saving, and applying wallpapers across multiple Windows monitors.
It exists so the codebase can talk about the wallpaper domain with consistent terms while evolving its seams.

## Language

**Monitor**:
A detected physical display that can be targeted by wallpaper operations.
_Avoid_: screen, output

**Wallpaper Source**:
The persisted source assigned to a Monitor: an image path, a solid-colour marker, or no wallpaper.
_Avoid_: file, asset, payload

**Wallpaper Draft**:
The current editable selection of Wallpaper Source and fit mode for one Monitor.
_Avoid_: config, temporary state

**Wallpaper Session**:
The in-memory working set of Monitor drafts, baseline state, previews, and apply operations for the current app run.
_Avoid_: app state, page state, controller

**Profile**:
A named saved set of Wallpaper Draft values keyed by Monitor.
_Avoid_: preset, template

**Preview**:
A visual rendering of a Wallpaper Source used in the UI before apply.
_Avoid_: thumbnail, cache entry

**Identify Overlay**:
A temporary on-screen label used to map a physical Monitor to its display index.
_Avoid_: badge, toast

## Relationships

- A **Wallpaper Session** tracks one **Wallpaper Draft** per active **Monitor**
- A **Wallpaper Draft** selects one **Wallpaper Source** and one fit mode for a **Monitor**
- A **Profile** stores many **Wallpaper Draft** values for later reuse
- A **Preview** visualises a **Wallpaper Source** inside a **Wallpaper Session**
- An **Identify Overlay** labels exactly one **Monitor** at a time

## Example dialogue

> **Dev:** "When a **Profile** is loaded, do we replace the whole **Wallpaper Session** or only the matching **Wallpaper Draft** values?"
> **Domain expert:** "We replace the matching **Wallpaper Draft** values for active **Monitor** entries, then keep editing from that **Wallpaper Session**."

## Flagged ambiguities

- "wallpaper" is used in code to mean both the persisted **Wallpaper Source** and the editable **Wallpaper Draft** — resolved: use **Wallpaper Source** for persisted input and **Wallpaper Draft** for editable state.
- "fit" and "fit mode" refer to the same concept — resolved: use **fit mode**.
