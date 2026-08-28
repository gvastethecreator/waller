---
description: "Use when editing the definitive WinUI application, Workflows, Core, native tests, packaging, or Windows integration."
name: "Native WinUI"
applyTo:
  - "native/**/*.cs"
  - "native/**/*.xaml"
  - "native/**/*.csproj"
  - "native/**/*.ps1"
  - "native/**/*.appxmanifest"
---
# Native WinUI rules

Follow [`AGENTS.md`](../../AGENTS.md). Path-specific extras:

- Keep Core independent from App, XAML, WinUI controls, and package identity.
- Keep Workflows dependent only on Core; no XAML or WinUI types in Workflows.
- Keep view models focused on UI projection; move multi-step product behavior behind Workflows seams.
- Keep Windows COM, shell, picker, package, and dispatcher details in App adapters.
- Serialize persistent writes and preserve atomic file replacement behavior.
- Extend the nearest public-seam test or existing guard when behavior changes.
- Run the root native verification command before handoff.
