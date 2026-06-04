# PROTOTYPE - WinUI Native Shell

## Question

Can Waller move from its current Tauri shell to a WinUI 3 native shell without rewriting the wallpaper domain all at once?

## Prototype Goal

Build a small WinUI 3 app that proves these flows before committing to a full migration:

- Detect active monitors.
- Show each monitor in a simple native list/grid.
- Pick an image with a native file picker.
- Apply one wallpaper to one monitor.
- Apply a full multi-monitor configuration.
- Save and load in-memory profile-like drafts.

The prototype should intentionally avoid production persistence. Its job is to validate the native app shape and the Windows API boundary.

## Intended Shape

The throwaway project lives under:

```text
prototypes/winui-native/
```

It was created with the WinUI MVVM template:

```powershell
dotnet new winui-mvvm -n WallerWinUIPrototype -o prototypes/winui-native
```

Run it with the WinUI workflow script from the WinUI skill:

```powershell
.\BuildAndRun.ps1
```

## First Implementation Slice

The first slice should prefer speed over reuse:

- Start with C# view models and services.
- Port only the monitor and wallpaper apply boundary from the current Rust backend.
- Keep profile data in memory.
- Skip the image editor entirely.
- Leave logging as debug output.

After the first slice runs, decide whether to:

- keep porting native Windows API calls into C#,
- expose the existing Rust backend as a library/CLI boundary,
- or abandon WinUI if the migration cost outweighs the native UX benefit.

## Current Prerequisite State

Checked on 2026-06-03:

- .NET SDK >= 8: present (`10.0.300`)
- Developer Mode: enabled
- WinUI 3 templates: installed in the elevated dotnet context
- WinApp CLI: available from the NuGet package cache, not from PATH
- Windows App Runtime 2.1.3: missing from installed AppX frameworks

The prototype scaffold was created and restored successfully. Build verification passes with:

```powershell
powershell -ExecutionPolicy Bypass -File .\BuildAndRun.ps1 -SkipRun
```

Running the app requires `winapp` to be available in PATH. The script will otherwise build and skip launch.
`BuildAndRun.ps1` has been adjusted to find `winapp.exe` from the local NuGet package cache when it is not in PATH.

Do not launch this file directly:

```text
prototypes/winui-native/bin/x64/Debug/net10.0-windows10.0.26100.0/win-x64/WallerWinUIPrototype.exe
```

Packaged WinUI apps need package identity during development. Launch through `BuildAndRun.ps1`, which delegates to `winapp run` after a successful build.

Current launch blocker:

```text
Windows cannot install package ... because this package depends on a framework that could not be found.
Provide the framework "Microsoft.WindowsAppRuntime.2" ... minimum version 2.1.3.0.
```

Manual `Add-AppxPackage` attempts against the runtime MSIX currently fail on this machine with `0x80070005` before package staging.

## Current Prototype State

- WinUI MVVM project scaffolded.
- Main page replaced with a Waller-specific native shell prototype.
- Monitor detection is mocked in memory.
- Wallpaper source, fit mode, profile save, clear, apply-selected, and apply-all are simulated.
- Full state is rendered in the UI after changes.
- No OS wallpaper is modified.

## Verdict Placeholder

Fill this in after the prototype is runnable:

- Result:
- Migration recommendation:
- Risks found:
- Code to keep:
- Code to delete:
