# Waller WinUI GitHub Research Report

**Research date:** 2026-06-04  
**Suggested repo path:** `docs/prototypes/winui/GITHUB_RESEARCH_REPORT.md`  
**Scope:** External GitHub/docs/package research for a fresh WinUI 3/.NET version of Waller. This report references the existing local Waller documents and does not duplicate them.

---

## 0. Local context anchor

The existing Waller app is a Windows-only multi-monitor wallpaper manager built with Tauri 2, Rust, React, TypeScript, Vite, Tailwind, and Bun. It already supports monitor detection through `IDesktopWallpaper`, per-monitor wallpaper sources, fit modes, profile JSON, previews, identify overlays, logs, and a lightweight editor.

The migration docs already make the most important architectural point: do not attempt a big-bang rewrite. The useful first slice is monitor detection + source selection + apply-one/apply-all in a WinUI prototype, using C# ViewModels/services and porting only the native wallpaper boundary first.

Relevant local docs to keep using as source of truth:

- `README.md`
- `docs/02-ARCHITECTURE.md`
- `docs/prototypes/winui/MIGRATION_REPORT.md`
- `docs/prototypes/winui/NOTES.md`
- `prototypes/winui-native/README.md`

---

## 1. Executive shortlist: top 5 resources to use

| Rank | Resource | Why it matters for Waller | Recommendation |
|---:|---|---|---|
| 1 | [CommunityToolkit.Mvvm](https://learn.microsoft.com/en-us/dotnet/communitytoolkit/mvvm/) + [MVVM Toolkit samples](https://github.com/CommunityToolkit/MVVM-Samples) | Best low-friction MVVM base for WinUI 3: `ObservableObject`, `[ObservableProperty]`, `[RelayCommand]`, messaging if needed, minimal architecture. | **Use directly.** |
| 2 | [Windows App SDK Samples](https://github.com/microsoft/WindowsAppSDK-Samples), especially [Windowing](https://github.com/microsoft/WindowsAppSDK-Samples/tree/main/Samples/Windowing) and [DeploymentManager](https://github.com/microsoft/WindowsAppSDK-Samples/tree/main/Samples/DeploymentManager) | Official reference for `AppWindow`, multi-window behavior, Windows App Runtime deployment checks, packaged/unpackaged implications. | **Use as official implementation reference.** |
| 3 | Official [`IDesktopWallpaper`](https://learn.microsoft.com/en-us/windows/win32/api/shobjidl_core/nn-shobjidl_core-idesktopwallpaper) docs + [microsoft/CsWin32](https://github.com/microsoft/CsWin32) + [rgl/SetWindowsDesktopWallpaper](https://github.com/rgl/SetWindowsDesktopWallpaper) | This is the cleanest path for per-monitor wallpaper control in C# without bringing a large desktop automation dependency. | **Implement Waller's own `WallpaperService` using CsWin32-generated COM/PInvoke. Copy patterns only.** |
| 4 | [`DisplayArea`](https://learn.microsoft.com/en-us/windows/windows-app-sdk/api/winrt/microsoft.ui.windowing.displayarea) + [`EnumDisplayMonitors`](https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-enumdisplaymonitors) + [`GetSystemMetrics`](https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-getsystemmetrics) | Required for monitor geometry, virtual-screen coordinates, negative coordinates, and placing identify overlays on the correct display. | **Use as the monitor/window positioning base.** |
| 5 | [PhotoSauce MagicScaler](https://github.com/saucecontrol/PhotoSauce) + [Win2D](https://github.com/microsoft/Win2D) + [SkiaSharp](https://github.com/mono/SkiaSharp) | Separate the image problem into preview generation now and editor canvas later. MagicScaler is strongest for thumbnails/resizing; Win2D or SkiaSharp are editor candidates. | **Use MagicScaler for previews if native decoding is not enough. Spike Win2D first for editor; keep SkiaSharp as fallback.** |

---

## 2. Executive recommendation

The current hypothesis is mostly correct:

```text
Build the new app as WinUI 3 + C#/.NET.
Port the wallpaper/profile/logging backend to C# services.
Use CommunityToolkit.Mvvm.
Defer the editor until monitor detection and apply-one/apply-all work.
Use the current Rust implementation only as behavioral reference.
```

### Changes I would make to the hypothesis

1. **Use CsWin32 as the first native interop path**, not a large wrapper dependency.
   - Reason: `IDesktopWallpaper`, `EnumDisplayMonitors`, `GetSystemMetrics`, and DPI helpers are a small enough surface to own directly.
   - CsWin32 generates strongly typed interop into the project and ships no bulky runtime assemblies.

2. **Do not port logging in the first real slice.**
   - For Phase 1, use debug output + UI status text.
   - Bring persistent logs back after real monitor detection/apply-one/apply-all works.

3. **Do not add Win2D, SkiaSharp, or MagicScaler in the first slice unless previews block validation.**
   - Built-in WinUI image loading is enough to select/apply paths.
   - Add MagicScaler when preview caching becomes real.
   - Add Win2D/SkiaSharp only during the editor spike.

4. **Treat per-monitor fit modes as a risk item.**
   - `IDesktopWallpaper.SetWallpaper(monitorId, path)` is per-monitor.
   - `IDesktopWallpaper.SetPosition(position)` appears to be system-wide display positioning.
   - If Waller needs independent fit/crop/zoom per monitor, the likely solution is pre-rendering monitor-sized images/PNGs/BMPs and applying those per monitor.

---

## 3. Direct answers to research questions

### 1. Existing WinUI 3 sample apps with good MVVM structure

Best references:

- [Official WinUI MVVM Toolkit tutorial](https://learn.microsoft.com/en-us/windows/apps/tutorials/winui-mvvm-toolkit/)
  - Demonstrates ViewModels, services, data binding, dependency injection, and unit testing around WinUI.
  - Good conceptual structure for Waller's `MainViewModel`, `MonitorViewModel`, and service layer.
- [CommunityToolkit/MVVM-Samples](https://github.com/CommunityToolkit/MVVM-Samples)
  - Good for source generator usage and command/property patterns.
  - Use snippets/patterns, not app architecture wholesale.
- [XamlBrewer WinUI 3 MVVM samples](https://github.com/XamlBrewer/WinUI3MasterDetailSample)
  - Useful for compact WinUI 3 desktop MVVM patterns with `x:Bind`.
  - Third-party sample; copy only concepts.
- [WinUI Gallery](https://github.com/microsoft/WinUI-Gallery)
  - Excellent for controls, layout, and XAML examples.
  - Not the right model for Waller's app architecture.

**Recommendation:** Use `CommunityToolkit.Mvvm` directly and keep architecture flatter than Template Studio. For Waller, prefer services + ViewModels over a generated navigation framework.

---

### 2. C# examples/libraries for `IDesktopWallpaper` per-monitor wallpaper control

Best references:

- Official [`IDesktopWallpaper`](https://learn.microsoft.com/en-us/windows/win32/api/shobjidl_core/nn-shobjidl_core-idesktopwallpaper)
- Official [`IDesktopWallpaper::SetWallpaper`](https://learn.microsoft.com/en-us/windows/win32/api/shobjidl_core/nf-shobjidl_core-idesktopwallpaper-setwallpaper)
- Official [`IDesktopWallpaper::GetWallpaper`](https://learn.microsoft.com/en-us/windows/win32/api/shobjidl_core/nf-shobjidl_core-idesktopwallpaper-getwallpaper)
- Official [`IDesktopWallpaper::SetPosition`](https://learn.microsoft.com/en-us/windows/win32/api/shobjidl_core/nf-shobjidl_core-idesktopwallpaper-setposition)
- [microsoft/CsWin32](https://github.com/microsoft/CsWin32)
- [rgl/SetWindowsDesktopWallpaper](https://github.com/rgl/SetWindowsDesktopWallpaper)
- [federico-paolillo/set-wallpaper](https://github.com/federico-paolillo/set-wallpaper)
- [EvotecIT/DesktopManager](https://github.com/EvotecIT/DesktopManager)
- [Vanara.Windows.Shell](https://www.nuget.org/packages/Vanara.Windows.Shell)

**Recommendation:** Implement your own thin `WallpaperService` with CsWin32. Use rgl and federico-paolillo as small references. Inspect DesktopManager only for edge cases. Avoid importing DesktopManager or Vanara in the MVP unless CsWin32 becomes too slow or painful.

---

### 3. Lightest reliable monitor enumeration / geometry / DPI approach

Recommended split:

| Need | Source |
|---|---|
| Wallpaper monitor identity | `IDesktopWallpaper.GetMonitorDevicePathCount`, `GetMonitorDevicePathAt`, `GetMonitorRECT` |
| Physical/virtual desktop geometry | `EnumDisplayMonitors` + `GetMonitorInfo` through CsWin32 |
| Whole virtual screen bounds | `GetSystemMetrics(SM_XVIRTUALSCREEN / SM_YVIRTUALSCREEN / SM_CXVIRTUALSCREEN / SM_CYVIRTUALSCREEN)` |
| WinUI window placement | Windows App SDK `DisplayArea` + `AppWindow.MoveAndResize` |
| DPI info | Prefer WinUI/AppWindow data where possible; use `GetDpiForWindow` or `GetDpiForMonitor` carefully depending on DPI awareness |

Important implementation rules:

- Keep native monitor coordinates in **pixels**.
- Do not clamp `left/top` to `0`; monitors to the left/above primary can produce negative virtual coordinates.
- Keep a separate UI scale/preview coordinate system for `MonitorLayout`.
- Use `IDesktopWallpaper.GetMonitorRECT` as wallpaper truth and GDI/DisplayArea as placement/context truth.
- Build a small correlation routine by rectangle equality/intersection, not by display index alone.

---

### 4. WinUI 3 multi-window overlays / always-on-top / borderless / positioning

Use:

- [Windows App SDK Windowing sample](https://github.com/microsoft/WindowsAppSDK-Samples/tree/main/Samples/Windowing)
- [`AppWindow`](https://learn.microsoft.com/en-us/windows/apps/windows-app-sdk/windowing/windowing-overview)
- [`OverlappedPresenter`](https://learn.microsoft.com/en-us/windows/windows-app-sdk/api/winrt/microsoft.ui.windowing.overlappedpresenter)
- [`OverlappedPresenter.IsAlwaysOnTop`](https://learn.microsoft.com/en-us/windows/windows-app-sdk/api/winrt/microsoft.ui.windowing.overlappedpresenter.isalwaysontop)
- [`DisplayArea`](https://learn.microsoft.com/en-us/windows/windows-app-sdk/api/winrt/microsoft.ui.windowing.displayarea)
- Optional: [dotMorten/WinUIEx](https://github.com/dotMorten/WinUIEx)

For the identify overlay:

```text
Create one Window per monitor.
Get the AppWindow for each Window.
Set OverlappedPresenter:
  - IsAlwaysOnTop = true
  - IsResizable = false
  - IsMaximizable = false
  - IsMinimizable = false
  - SetBorderAndTitleBar(false, false)
MoveAndResize to the monitor's pixel bounds.
Render monitor number + simple instruction.
Close all overlay windows after timeout or Escape.
```

**Recommendation:** Start with raw `AppWindow`/`OverlappedPresenter`. Add WinUIEx only if repeated HWND/windowing boilerplate becomes noisy.

---

### 5. Lightweight image libraries for preview generation and edit/export

| Library | Best use | Recommendation |
|---|---|---|
| WinUI built-in `BitmapImage` / `Image` | Display selected image quickly | Use in Phase 1 if preview generation is not cached/exported. |
| [PhotoSauce MagicScaler](https://github.com/saucecontrol/PhotoSauce) | High-quality resize/thumbnail/cache generation | Best preview pipeline candidate. |
| [Win2D](https://github.com/microsoft/Win2D) | Native GPU 2D canvas/effects/export path | Best Windows-native editor candidate. |
| [SkiaSharp](https://github.com/mono/SkiaSharp) | Cross-platform 2D canvas, mature image API, MIT | Strong editor fallback if Win2D friction appears. |
| [ImageSharp](https://www.nuget.org/packages/SixLabors.ImageSharp) | Server-side or pure managed image operations | Avoid as default because of Split License/commercial constraints. |

---

### 6. Best fit for editor path

**Recommended editor strategy:**

1. **Do not implement editor in the MVP.**
2. **Use MagicScaler for preview/cache resize if needed before editor.**
3. **Run a Win2D spike for the editor path.**
   - Crop/pan/zoom/rotate map naturally to canvas transforms.
   - Tint/filter path can use Win2D effects and/or composition effects.
   - Export can be done by rendering to an offscreen target.
4. **If Win2D gets painful, spike SkiaSharp.**
   - SkiaSharp has a mature 2D drawing/image model and MIT license.
   - It may add more package/platform surface than Win2D but is a strong fallback.
5. **Do not choose ImageSharp unless the licensing decision is explicit.**
   - The package is high quality, but the Split License is not as clean as MIT/Apache/BSD for a generic open-source/commercial future.

---

### 7. Small WinUI 3 file-picker/profile/settings examples

Use the official Windows App SDK picker API:

- [Open files/folders with a picker](https://learn.microsoft.com/en-us/windows/apps/develop/files/pickers-save-file)
  - New pattern creates pickers with `new FileOpenPicker(this.AppWindow.Id)`.
  - Returns lightweight result objects with file paths.
  - This is a better fit for Waller than older HWND-initialized `Windows.Storage.Pickers` patterns.

For profiles/settings:

- Use `System.Text.Json`.
- Store in the existing Waller folder contract: `%APPDATA%/WallpaperManager/profiles`.
- Keep the profile JSON schema and source markers stable:
  - absolute image path
  - `__NONE__`
  - `__SOLID__:#RRGGBB`

Avoid pulling a settings framework unless there is a real need for roaming, encryption, or complex preferences.

---

### 8. Repositories/libraries to avoid or only use as conceptual reference

| Resource | Avoid / caution reason |
|---|---|
| [rocksdanister/lively](https://github.com/rocksdanister/lively) | GPL-3.0, large animated wallpaper product, not a small static wallpaper manager reference. Useful for product inspiration only, not code reuse. |
| [Microsoft/TemplateStudio](https://github.com/microsoft/TemplateStudio) | Useful historically, but overbuilt for Waller and appears behind current Windows App SDK pace. Do not base the new app on it. |
| [SixLabors ImageSharp](https://www.nuget.org/packages/SixLabors.ImageSharp) | Good library, but Split License/commercial conditions make it a poor default dependency unless the team explicitly accepts the license. |
| [EvotecIT/DesktopManager](https://github.com/EvotecIT/DesktopManager) | Active and impressive, but far broader than Waller: PowerShell module, CLI, MCP server, window/monitor/brightness/screenshot APIs. Inspect only. |
| [Vanara.Windows.Shell](https://www.nuget.org/packages/Vanara.Windows.Shell) | Mature P/Invoke wrapper, but heavier than needed for the MVP. Use only if CsWin32 interop becomes expensive. |
| Random `IDesktopWallpaper` gists | Useful for understanding the COM shape, but often incomplete, unmaintained, and license-unclear. Do not import. |
| Old UWP-only samples/guidance | Mark as conceptual only. Waller is WinUI 3 / Windows App SDK desktop, not UWP. |
| `SystemParametersInfo(SPI_SETDESKWALLPAPER)`-only examples | Usually set one wallpaper globally and miss per-monitor behavior. Use `IDesktopWallpaper` instead. |

---

## 4. Candidate review table

| Name/link | Category | Fit | What it could save | License | Maintenance | Dependencies | WinUI relevance | Risk | Recommendation |
|---|---|---:|---|---|---|---|---|---|---|
| [CommunityToolkit.Mvvm](https://learn.microsoft.com/en-us/dotnet/communitytoolkit/mvvm/) | MVVM toolkit | High | Observable properties, commands, messaging, testable VMs | MIT | Active Microsoft/community toolkit | Small NuGet | WinUI 3 compatible | Source generator learning curve | **Use** |
| [CommunityToolkit/MVVM-Samples](https://github.com/CommunityToolkit/MVVM-Samples) | MVVM samples | High | Correct generator patterns | MIT | Active enough as reference | Toolkit only | Cross-framework, applicable to WinUI | Samples are generic | **Copy patterns only** |
| [WinUI MVVM Toolkit tutorial](https://learn.microsoft.com/en-us/windows/apps/tutorials/winui-mvvm-toolkit/) | Official tutorial | High | VM/service/DI/unit-test shape | Microsoft docs | Current docs | Toolkit | WinUI-specific | Tutorial scale only | **Use as structure reference** |
| [WinUI Gallery](https://github.com/microsoft/WinUI-Gallery) | Controls/layout | Medium | XAML controls, states, accessibility cues | MIT | Official Microsoft sample | Windows App SDK | WinUI 3 direct | Not MVVM app architecture | **Use for UI patterns** |
| [WindowsAppSDK-Samples](https://github.com/microsoft/WindowsAppSDK-Samples) | Official samples | High | Deployment, windowing, runtime, packaging references | MIT | Official Microsoft sample set | Windows App SDK | Direct | Samples are fragmented | **Use** |
| [Windowing sample](https://github.com/microsoft/WindowsAppSDK-Samples/tree/main/Samples/Windowing) | Windowing/overlays | High | `AppWindow`, presenters, positioning | MIT | Official | Windows App SDK | Direct | Need adapt to overlay lifecycle | **Use** |
| [DeploymentManager sample](https://github.com/microsoft/WindowsAppSDK-Samples/tree/main/Samples/DeploymentManager) | Runtime deployment | Medium | Runtime bootstrap/deployment checks | MIT | Official | Windows App SDK | Direct | More relevant for unpackaged/runtime edge cases | **Investigate for packaging** |
| [Microsoft.WindowsAppSDK NuGet](https://www.nuget.org/packages/Microsoft.WindowsAppSDK) | Runtime/package | High | Confirms current package/runtime dependency | Microsoft package | Current | Windows App Runtime | Direct | Local machine runtime install already blocked once | **Pin/document clearly** |
| [Windows App SDK file pickers](https://learn.microsoft.com/en-us/windows/apps/develop/files/pickers-save-file) | File picker | High | Native file open/save/folder picking | Microsoft docs | Current | Windows App SDK | Direct | New API differs from older HWND picker pattern | **Use** |
| [microsoft/CsWin32](https://github.com/microsoft/CsWin32) | Interop generator | High | Typed COM/PInvoke without handwritten signatures | MIT | Active Microsoft project | Build-time generator | Generic .NET; ideal for WinUI service | Generated code can be noisy | **Use** |
| [`IDesktopWallpaper` docs](https://learn.microsoft.com/en-us/windows/win32/api/shobjidl_core/nn-shobjidl_core-idesktopwallpaper) | Wallpaper API | High | Official contract for monitor IDs/wallpaper state | Microsoft docs | Stable Win32 API | COM | Desktop app direct | `SetPosition` global semantics must be tested | **Use** |
| [rgl/SetWindowsDesktopWallpaper](https://github.com/rgl/SetWindowsDesktopWallpaper) | Wallpaper API sample | Medium | CsWin32 + `IDesktopWallpaper` reference | Repo license not obvious in quick pass; verify | Small, few commits, no releases | CsWin32, .NET 6 | Generic .NET | Tiny sample; not a library | **Copy pattern only** |
| [federico-paolillo/set-wallpaper](https://github.com/federico-paolillo/set-wallpaper) | Wallpaper API sample | Medium | Minimal C# COM wrapper for get/set by monitor path | MIT | Small sample | Handmade COM | Generic .NET | Only subset of interface | **Copy pattern only** |
| [EvotecIT/DesktopManager](https://github.com/EvotecIT/DesktopManager) | Desktop/monitor/wallpaper library | Medium | Lots of edge-case code to inspect | MIT-like? verify repo | Very active; release seen in 2026 | Broad PowerShell/CLI/MCP/core stack | Generic .NET | Overbuilt for Waller MVP | **Investigate source, avoid dependency** |
| [Vanara.Windows.Shell](https://www.nuget.org/packages/Vanara.Windows.Shell) | Win32/Shell wrapper | Medium | Existing Shell/PInvoke wrappers | MIT | Active; recent packages | Vanara packages | Generic .NET | More dependency surface than needed | **Fallback only** |
| [`DisplayArea`](https://learn.microsoft.com/en-us/windows/windows-app-sdk/api/winrt/microsoft.ui.windowing.displayarea) | Display/window positioning | High | Work area, outer bounds, finding display for rect/window | Microsoft docs | Current Windows App SDK | Windows App SDK | Direct | Must correlate with wallpaper monitor IDs | **Use** |
| [`EnumDisplayMonitors`](https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-enumdisplaymonitors) | Monitor enumeration | High | Monitor handles + rectangles | Microsoft docs | Stable Win32 | User32 via CsWin32 | Generic desktop | Interop details | **Use** |
| [`GetSystemMetrics`](https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-getsystemmetrics) | Virtual screen bounds | High | Virtual desktop coordinate bounds | Microsoft docs | Stable Win32 | User32 via CsWin32 | Generic desktop | Need avoid primary-only metrics | **Use** |
| [`GetDpiForMonitor`](https://learn.microsoft.com/en-us/windows/win32/api/shellscalingapi/nf-shellscalingapi-getdpiformonitor) | DPI | Medium | Monitor DPI when needed | Microsoft docs | Stable Win32 | Shcore via CsWin32 | Generic desktop | Returns depend on DPI awareness; `GetDpiForWindow` may be better | **Use carefully** |
| [dotMorten/WinUIEx](https://github.com/dotMorten/WinUIEx) | WinUI helper library | Medium | Window helpers, HWND helpers, tray/icon helpers | MIT | Active; release seen in 2026 | WinUIEx | Direct WinUI 3 | Extra dependency; titlebar helpers partially superseded | **Optional later** |
| [XamlBrewer multi-window sample](https://xamlbrewer.wordpress.com/2022/05/30/a-winui-3-desktop-mvvm-app-with-multiple-windows/) | Multi-window sample | Medium | Practical multi-window + MVVM + WinUIEx pattern | Blog sample | Older but useful | MVVM Toolkit, WinUIEx | Direct | Third-party sample | **Copy concept only** |
| [Microsoft Win2D](https://github.com/microsoft/Win2D) | 2D graphics/editor | Medium/High | Native GPU image rendering/effects/export path | MIT | Official project; sample docs lag WinUI 3 | Win2D NuGet | Direct WinUI 3 support | Docs say some sample code not updated for WinUI 3 | **Spike for editor** |
| [SkiaSharp](https://github.com/mono/SkiaSharp) | 2D graphics/editor | Medium/High | Mature drawing/image operations, cross-platform 2D | MIT | Active; release seen in 2026 | SkiaSharp packages | WinUI 3 supported | More package surface; sample glue may need adaptation | **Fallback/spike** |
| [XamlBrewer SkiaSharp sample](https://github.com/XamlBrewer/WinUI3SkiaSharpSample) | Skia WinUI sample | Medium | Shows `SKXamlCanvas` in WinUI 3 | Repo license/check before copy | Small sample, no releases | SkiaSharp | Direct | Third-party, sample not library | **Copy pattern only** |
| [PhotoSauce MagicScaler](https://github.com/saucecontrol/PhotoSauce) | Image resize/previews | High for previews | High-quality thumbnail/cache generation | MIT | Active enough; NuGet available | MagicScaler, codecs optional | Generic .NET | Not an interactive editor | **Use for preview pipeline** |
| [SixLabors ImageSharp](https://www.nuget.org/packages/SixLabors.ImageSharp) | Image processing | Low/Medium | Pure managed image operations | Six Labors Split License | Active | ImageSharp | Generic .NET | Commercial/license ambiguity for future product | **Avoid by default** |
| [rocksdanister/lively](https://github.com/rocksdanister/lively) | Wallpaper app | Low | Product-level inspiration for monitor wallpaper UX | GPL-3.0 | Active/public | Large app stack | WinUI 3 UI, but product is broader | GPL and overbuilt | **Do not use code** |
| [Microsoft/TemplateStudio](https://github.com/microsoft/TemplateStudio) | App scaffolding | Low/Medium | Generated navigation/settings examples | Verify license before copy | Appears behind current Windows App SDK pace | Many generated packages | WinUI 3 support exists | Overbuilt/stale for Waller | **Do not base project on it** |

---

## 5. Recommended stack for the new WinUI app

### MVVM/toolkit

```text
CommunityToolkit.Mvvm
```

Use:

- `ObservableObject`
- `[ObservableProperty]`
- `[RelayCommand]`
- `ObservableCollection<T>`
- optional `IMessenger` only if multi-window overlay communication becomes useful

Avoid:

- Prism
- ReactiveUI
- Template Studio generated navigation shell
- heavy DI until the service set stabilizes

A simple constructor-injected VM/service layout is enough.

---

### Monitor/wallpaper API approach

Use:

```text
Microsoft.Windows.CsWin32
IDesktopWallpaper
EnumDisplayMonitors
GetMonitorInfo
GetSystemMetrics
DisplayArea
AppWindow
OverlappedPresenter
```

Suggested service split:

```text
Services/
  IWallpaperService.cs
  WallpaperService.cs
  IMonitorService.cs
  MonitorService.cs
  IFilePickerService.cs
  FilePickerService.cs
  IProfileService.cs
  JsonProfileService.cs
```

Suggested model split:

```text
Models/
  MonitorSnapshot.cs
  MonitorRect.cs
  WallpaperDraft.cs
  WallpaperSource.cs
  WallpaperFitMode.cs
  WallpaperProfile.cs
```

Interop file:

```text
NativeMethods.txt
```

Initial API names to include:

```text
IDesktopWallpaper
EnumDisplayMonitors
GetMonitorInfo
GetSystemMetrics
GetDpiForMonitor
GetDpiForWindow
MonitorFromWindow
RECT
MONITORINFO
MONITORINFOEX
SM_XVIRTUALSCREEN
SM_YVIRTUALSCREEN
SM_CXVIRTUALSCREEN
SM_CYVIRTUALSCREEN
```

Validate exact CsWin32 symbol names during implementation.

---

### Preview/image library

Phase 1:

```text
No image library dependency unless necessary.
Use WinUI Image/BitmapImage to display picked image paths.
```

Phase 2 previews:

```text
PhotoSauce.MagicScaler
```

Use MagicScaler for:

- bounded previews
- profile thumbnail cache
- fast, high-quality resize
- future export normalization

---

### Editor strategy

Recommended order:

1. Defer editor from MVP.
2. Build preview/cache pipeline first.
3. Spike Win2D editor:
   - image load
   - pan/zoom
   - rotate
   - crop/export PNG
   - tint/filter
4. If Win2D package or sample friction is high, spike SkiaSharp with `SKXamlCanvas`.
5. Do not choose ImageSharp unless the team explicitly accepts its license.

---

### Packaging/runtime strategy

Use the current packaged WinUI development flow until there is a reason to change it:

```text
BuildAndRun.ps1
winapp run
Windows App SDK runtime documented/pinned
```

Because the local prototype already hit a Windows App Runtime dependency blocker, keep a short `docs/prototypes/winui/RUNTIME_SETUP.md` next to the prototype with:

- required Windows App SDK package version
- exact NuGet package version
- `BuildAndRun.ps1 -SkipRun`
- how to repair/update Windows App Runtime
- known `0x80070005` package staging failure notes
- why launching the raw `.exe` is not valid for the packaged dev build

For release planning, decide later between:

- packaged MSIX
- unpackaged app with bootstrap/deployment manager
- self-contained deployment if runtime friction is unacceptable

Do not defer packaging validation until the end.

---

## 6. Proposed first implementation slice

Goal:

```text
Replace the mock monitor list and simulated apply actions with real monitor detection and apply-one/apply-all.
No persistent profiles.
No editor.
No preview cache.
No release packaging changes.
```

### Package references

Add only:

```xml
<PackageReference Include="CommunityToolkit.Mvvm" Version="latest-compatible" />
<PackageReference Include="Microsoft.Windows.CsWin32" Version="latest-compatible" PrivateAssets="all" />
```

Keep existing `Microsoft.WindowsAppSDK` package pinned to the prototype's known-good version unless the runtime issue requires an upgrade.

Do not add:

```text
Win2D
SkiaSharp
MagicScaler
ImageSharp
WinUIEx
Vanara
DesktopManager
```

in this first slice.

### Files/classes to create

```text
Models/
  MonitorSnapshot.cs
  MonitorRect.cs
  WallpaperFitMode.cs
  WallpaperSource.cs
  WallpaperDraft.cs

Services/
  IMonitorService.cs
  MonitorService.cs
  IWallpaperService.cs
  WallpaperService.cs
  IFilePickerService.cs
  FilePickerService.cs

ViewModels/
  MonitorViewModel.cs
  MainViewModel.cs

Interop/
  NativeMethods.txt
```

If the current prototype already has a VM layout, adapt names rather than forcing these exact paths.

### Implementation sequence

1. **Add native interop generator**
   - Add `Microsoft.Windows.CsWin32`.
   - Add `NativeMethods.txt`.
   - Generate `IDesktopWallpaper` + monitor APIs.

2. **Implement `WallpaperService.GetMonitorsAsync()`**
   - Create `IDesktopWallpaper`.
   - Call `GetMonitorDevicePathCount`.
   - For each index:
     - `GetMonitorDevicePathAt(index)`
     - `GetMonitorRECT(monitorId)`
     - `GetWallpaper(monitorId)`
   - Return `MonitorSnapshot`.

3. **Add GDI/Display correlation**
   - Call `EnumDisplayMonitors`.
   - Gather monitor rectangles and primary/work area/device name if available.
   - Correlate to `IDesktopWallpaper` monitor rects.
   - Keep wallpaper monitor ID as the stable apply key.

4. **Replace mock monitors**
   - `MainViewModel.LoadMonitorsCommand`
   - `ObservableCollection<MonitorViewModel>`
   - UI displays real geometry and current wallpaper path.

5. **Implement file picker**
   - Use Windows App SDK `FileOpenPicker(AppWindow.Id)`.
   - Restrict to image extensions initially:
     - `.jpg`
     - `.jpeg`
     - `.png`
     - `.bmp`
     - `.webp` if decoder support is confirmed

6. **Implement apply-one**
   - Convert selected `WallpaperSource`.
   - If path: call `SetWallpaper(monitorId, fullPath)`.
   - If `__NONE__`: decide temporary behavior:
     - either clear/disable through API if safe,
     - or show "not implemented in first slice".
   - If `__SOLID__:#RRGGBB`: generate temporary solid BMP sized to monitor, then apply that path. Do not use global background color if per-monitor behavior matters.

7. **Implement apply-all**
   - Iterate current monitor drafts.
   - Apply each monitor by monitor ID.
   - Apply global `SetPosition(fitMode)` only after deciding whether Waller's fit mode is global or per-monitor.
   - If independent fit mode is required, defer and document that pre-rendered monitor images are required.

8. **Add smoke diagnostics**
   - UI status messages.
   - Debug output for COM HRESULTs.
   - Do not add full persistent logging yet.

### Sample files/patterns to mimic

- Windowing:
  - <https://github.com/microsoft/WindowsAppSDK-Samples/tree/main/Samples/Windowing>
- Runtime/deployment:
  - <https://github.com/microsoft/WindowsAppSDK-Samples/tree/main/Samples/DeploymentManager>
- MVVM:
  - <https://learn.microsoft.com/en-us/windows/apps/tutorials/winui-mvvm-toolkit/>
  - <https://github.com/CommunityToolkit/MVVM-Samples>
- Wallpaper:
  - <https://github.com/rgl/SetWindowsDesktopWallpaper>
  - <https://github.com/federico-paolillo/set-wallpaper>
- Future image/editor:
  - <https://github.com/saucecontrol/PhotoSauce>
  - <https://github.com/microsoft/Win2D>
  - <https://github.com/XamlBrewer/WinUI3SkiaSharpSample>

---

## 7. Open questions requiring human decision

1. **Does Waller require independent fit mode per monitor?**
   - If yes, `IDesktopWallpaper.SetPosition` may not be enough.
   - Likely solution: pre-render monitor-sized images and apply those per monitor.

2. **Should solid color be per-monitor or global?**
   - Existing contract supports `__SOLID__:#RRGGBB`.
   - Current architecture mentions a solid-color BMP cache.
   - Recommended: preserve per-monitor semantics by generating solid BMPs.

3. **How much profile compatibility is mandatory for the first public WinUI prototype?**
   - Keep the current source marker contract.
   - Decide whether to load existing Tauri profiles immediately or only after apply-one/apply-all works.

4. **Packaged-only or unpackaged deployment?**
   - Packaged is simpler for WinUI identity and current prototype shape.
   - Unpackaged may reduce install friction but requires runtime/bootstrap planning.

5. **Win2D or SkiaSharp for editor?**
   - Recommendation: Win2D spike first.
   - Decision should be based on a 1-day editor proof:
     - load image
     - zoom/pan
     - crop
     - rotate
     - tint/filter
     - export PNG

6. **Should WinUIEx be allowed?**
   - Keep out of MVP.
   - Allow it later if overlays/tray/HWND helpers become repetitive.

7. **What is the long-term distribution target?**
   - GitHub release/MSIX?
   - Microsoft Store?
   - Installer?
   - Portable build?
   - This affects runtime strategy, signing, and auto-update choices.

---

## 8. Final recommendation

Proceed with the WinUI experiment, but keep it deliberately narrow:

```text
WinUI 3 + C#/.NET
CommunityToolkit.Mvvm
CsWin32-generated IDesktopWallpaper + monitor APIs
No Rust interop
No editor dependency yet
No heavy desktop automation library
No Template Studio base
No ImageSharp default dependency
```

The next implementation slice should prove:

```text
real monitor detection
real current wallpaper read
pick image
apply selected monitor
apply all monitors
show geometry/current wallpaper state
```

If this slice is stable, the migration remains worth pursuing. If this slice becomes painful around COM, runtime/package identity, or monitor geometry, then hardening the existing Tauri app is still cheaper than forcing a full rewrite.
