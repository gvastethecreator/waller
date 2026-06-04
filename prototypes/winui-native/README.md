# Waller WinUI Prototype

This is a throwaway WinUI 3 prototype for testing a native Waller shell.

## Build

```powershell
powershell -ExecutionPolicy Bypass -File .\BuildAndRun.ps1 -SkipRun
```

## Run

```powershell
powershell -ExecutionPolicy Bypass -File .\BuildAndRun.ps1
```

The app cannot be launched reliably by double-clicking the generated `.exe` in `bin/`.
Packaged WinUI apps need package identity during development, and this script launches through `winapp run`.

If the script says `winapp CLI not found in PATH`, run the WinUI setup flow first.

If launch fails with `Microsoft.WindowsAppRuntime.2` missing, install the Windows App Runtime 2.x framework first. The project uses `Microsoft.WindowsAppSDK` 2.1.3, so a machine with only Windows App Runtime 1.x will build successfully but fail before showing a window.
