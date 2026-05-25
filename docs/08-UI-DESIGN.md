# UI/UX Design

## Current design principles

- **Single-window workflow:** keep the main wallpaper flow in one place.
- **Fast monitor-level actions:** each monitor card should make it obvious how to browse, clear, edit, and apply.
- **Immediate feedback:** use Preview state, dirty badges, toasts, logs, and identify highlighting.
- **Operational clarity over decoration:** the UI should help users understand what will be applied and what is still pending.

## UI structure

### Header

- app title
- status pill summarizing monitor count and pending changes
- locale switcher (EN/ES)
- refresh button

### Profile/action bar

- profile selector
- load/save/delete profile actions
- view logs / clear logs actions

### Main workspace

- monitor layout overview (`MonitorLayout`)
- Identify Overlay trigger
- diagnostic-mode warning when fallback monitor IDs are active
- responsive grid of `MonitorCard` components

### Footer

- single prominent `Apply Configuration` call to action

### Dialogs and overlays

- save-profile modal
- logs modal
- `EditorDialog`
- toast notifications

## Monitor card anatomy

Each `MonitorCard` currently shows:

- monitor index and name
- dirty badge when the Wallpaper Draft differs from the applied baseline
- preview area respecting the active fit mode
- Wallpaper Source selector
- solid-colour picker when applicable
- fit-mode selector
- clear / edit / apply buttons

## Preview states

The UI currently distinguishes between:

- ready image preview
- preview loading
- preview error / unavailable preview
- solid-colour preview
- no-wallpaper preview

That state maps directly to the `Preview` contract exposed by `wallpaperSession.ts`.

## Editor UX

The built-in editor is intentionally lightweight and wallpaper-focused. It supports:

- drag-to-pan
- wheel zoom
- rotation
- brightness / contrast / saturation / hue
- blur
- tint colour + tint strength
- save-and-apply as edited PNG

It is not intended to become a full image authoring suite.

## Current strengths

- Clear monitor-level affordances
- Good visibility of unapplied changes
- Logs are accessible without leaving the main flow
- Identify Overlay supports real-world monitor mapping
- The editor fits inside the product without turning the app into a separate design tool

## Current UX opportunities

- Surface `health_check` in a support/debug view.
- Add more explicit per-monitor success/failure feedback after apply.
- Add a small explanation/tooltip for diagnostic mode and fallback monitor IDs.
- Improve keyboard/accessibility coverage around modals and the editor controls.
