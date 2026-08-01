# ADR 0004: Presets Are Local App-Managed JSON

Date: 2026-06-04

## Status

Accepted.

## Context

The old app has JSON-style profile concepts. For Waller Native, the user wants
local in-app saved configurations, not manual import/export workflows.

## Decision

Use "Preset" as product language.

Store Presets as app-managed JSON under:

```text
%LOCALAPPDATA%\Waller\presets
```

Do not expose manual JSON import/export in MVP.

## Consequences

Positive:

- simpler UX
- no file round-trip burden for user
- versioned storage remains inspectable for dev
- future migration/import can be built as a separate adapter

Negative:

- users cannot manually sync/export in MVP
- storage schema must be maintained by app

## Follow-Up

Add schema versioning and migration tests before any public release.

