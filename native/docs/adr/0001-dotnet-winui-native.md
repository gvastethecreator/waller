# ADR 0001: Use C#/.NET WinUI For Waller Native

Date: 2026-06-04

## Status

Accepted.

## Context

Current Waller app is Tauri/Rust/Web UI. The new project is intentionally a
fresh native Windows app, not a direct migration of the current UI.

The team considered whether to keep Rust, move to C/C++, or use C#/.NET.

## Decision

Use C#/.NET with WinUI 3.

Use existing Rust code only as behavior reference.

## Consequences

Positive:

- native Fluent UI path
- fewer moving parts than Tauri + Rust + web runtime
- good fit for Windows App SDK
- Core behavior can be tested with normal .NET tests
- Windows interop can use CsWin32

Negative:

- current Rust implementation is not reused directly
- some wallpaper behavior must be reimplemented in C#
- packaged WinUI development has runtime/identity quirks

## Follow-Up

Keep references to old behavior in docs and tests, not as runtime dependency.

