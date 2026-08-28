# Documentation Index

Waller's definitive product is the WinUI 3 solution under `native/`. This index lists only active documentation and durable architecture records.

## Product and contributor entry points

- [`../README.md`](../README.md) — product, setup, root commands, local data, and release gates
- [`../CONTEXT.md`](../CONTEXT.md) — canonical product vocabulary
- [`../CONTRIBUTING.md`](../CONTRIBUTING.md) — contribution and verification workflow
- [`../SECURITY.md`](../SECURITY.md) — native trust boundary and vulnerability reporting
- [`DEPENDENCY_UPDATES.md`](./DEPENDENCY_UPDATES.md) — current NuGet versions and upstream release notes
- [`QUALITY_AUDIT.md`](./QUALITY_AUDIT.md) — maintenance verification and residual gates
- [`CHANGELOG_MAINTENANCE.md`](./CHANGELOG_MAINTENANCE.md) — maintenance decisions and follow-up work

## Native implementation

- [`../native/README.md`](../native/README.md) — current native implementation and command reference
- [`../native/docs/README.md`](../native/docs/README.md) — native documentation map
- [`../native/docs/ARCHITECTURE.md`](../native/docs/ARCHITECTURE.md) — module boundaries and dependency direction
- [`../native/docs/TESTING.md`](../native/docs/TESTING.md) — verification and smoke strategy
- [`../native/docs/PACKAGING.md`](../native/docs/PACKAGING.md) — package, certificate, install, and release policy
- [`../native/docs/STATUS.md`](../native/docs/STATUS.md) — current implementation status and known gates
- [`../native/docs/IMPLEMENTATION_PLAN.md`](../native/docs/IMPLEMENTATION_PLAN.md) — native delivery history and remaining work
- [`../native/docs/WINDOWS_INTEROP.md`](../native/docs/WINDOWS_INTEROP.md) — Windows API boundaries and failure modes

## Architecture batch

- [`architecture/winui-definitive-architecture-spec.md`](./architecture/winui-definitive-architecture-spec.md) — approved definitive-WinUI specification
- [`architecture/WORKPLAN.md`](./architecture/WORKPLAN.md) — ten-ticket dependency tracker and evidence ledger
- [`architecture/issues/`](./architecture/issues/) — implementation-ready architecture tickets
- [`architecture/winui-definitive-architecture-report.md`](./architecture/winui-definitive-architecture-report.md) — completed batch, verification, and residual risk
- [`architecture/winui-definitive-architecture-report.html`](./architecture/winui-definitive-architecture-report.html) — visual completion report

## Decision records

- [`../native/docs/adr/0001-dotnet-winui-native.md`](../native/docs/adr/0001-dotnet-winui-native.md) — historical decision to replace the retired Tauri product with C#/.NET and WinUI
- [`../native/docs/adr/0002-core-is-windows-only.md`](../native/docs/adr/0002-core-is-windows-only.md) — Windows-only Core
- [`../native/docs/adr/0003-prerender-per-monitor-wallpapers.md`](../native/docs/adr/0003-prerender-per-monitor-wallpapers.md) — per-monitor PNG rendering
- [`../native/docs/adr/0004-presets-local-app-managed-json.md`](../native/docs/adr/0004-presets-local-app-managed-json.md) — local Preset persistence
- [`../native/docs/adr/0005-apply-and-save-are-independent.md`](../native/docs/adr/0005-apply-and-save-are-independent.md) — independent Apply and Save semantics
