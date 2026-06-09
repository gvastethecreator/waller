# ADR 0002: Waller.Native.Core Is Windows-Only

Date: 2026-06-04

## Status

Accepted.

## Context

Core could be made cross-platform in theory, but Waller Native is a Windows
wallpaper manager. The product needs Windows monitor and wallpaper APIs.

## Decision

Target Core as Windows-only:

```text
net10.0-windows10.0.26100.0
```

## Consequences

Positive:

- simpler API boundaries
- Windows types and assumptions are allowed in Core
- less adapter ceremony
- tests still run as .NET tests on Windows

Negative:

- Core cannot be reused on non-Windows platforms
- CI needs Windows for full coverage

## Follow-Up

Do not add fake cross-platform abstractions. Add interfaces only where they help
testing or isolate Windows interop.

