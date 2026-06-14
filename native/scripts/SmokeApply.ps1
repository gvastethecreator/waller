param(
    [string]$ProjectPath = ".\Waller.Native.App\Waller.Native.App.csproj",
    [int]$LaunchTimeoutSeconds = 10,
    [int]$ApplyTimeoutSeconds = 25,
    [switch]$DisableNuGetAudit
)

$ErrorActionPreference = "Stop"

$nativeRoot = Split-Path -Parent $PSScriptRoot
$buildScript = Join-Path $nativeRoot "BuildAndRun.ps1"
$appProcessId = $null
$wallpaperBackup = $null
$localDataRoot = $null

Add-Type -TypeDefinition @"
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

public enum SmokeDesktopWallpaperPosition
{
    Center = 0,
    Tile = 1,
    Stretch = 2,
    Fit = 3,
    Fill = 4,
    Span = 5,
}

public sealed class SmokeWallpaperMonitor
{
    public string MonitorId { get; set; }
    public string WallpaperPath { get; set; }
}

public sealed class SmokeWallpaperBackup
{
    public SmokeDesktopWallpaperPosition Position { get; set; }
    public uint BackgroundColor { get; set; }
    public SmokeWallpaperMonitor[] Monitors { get; set; }
}

[ComImport]
[Guid("B92B56A9-8B55-4E14-9A89-0199BBB6F93B")]
[InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
public interface ISmokeDesktopWallpaper
{
    [PreserveSig]
    int SetWallpaper(
        [MarshalAs(UnmanagedType.LPWStr)] string monitorId,
        [MarshalAs(UnmanagedType.LPWStr)] string wallpaper);

    [PreserveSig]
    int GetWallpaper(
        [MarshalAs(UnmanagedType.LPWStr)] string monitorId,
        out IntPtr wallpaper);

    [PreserveSig]
    int GetMonitorDevicePathAt(uint monitorIndex, out IntPtr monitorId);

    [PreserveSig]
    int GetMonitorDevicePathCount(out uint count);

    [PreserveSig]
    int GetMonitorRECT(string monitorId, out SmokeDesktopWallpaperRect displayRect);

    [PreserveSig]
    int SetBackgroundColor(uint color);

    [PreserveSig]
    int GetBackgroundColor(out uint color);

    [PreserveSig]
    int SetPosition(SmokeDesktopWallpaperPosition position);

    [PreserveSig]
    int GetPosition(out SmokeDesktopWallpaperPosition position);

    [PreserveSig]
    int SetSlideshow(IntPtr items);

    [PreserveSig]
    int GetSlideshow(out IntPtr items);

    [PreserveSig]
    int SetSlideshowOptions(uint options, uint slideshowTick);

    [PreserveSig]
    int GetSlideshowOptions(out uint options, out uint slideshowTick);

    [PreserveSig]
    int AdvanceSlideshow(string monitorId, uint direction);

    [PreserveSig]
    int GetStatus(out uint state);

    [PreserveSig]
    int Enable([MarshalAs(UnmanagedType.Bool)] bool enable);
}

[StructLayout(LayoutKind.Sequential)]
public struct SmokeDesktopWallpaperRect
{
    public int Left;
    public int Top;
    public int Right;
    public int Bottom;
}

public static class SmokeDesktopWallpaperInterop
{
    private static readonly Guid DesktopWallpaperClassId = new Guid("C2CF3110-460E-4FC1-B9D0-8A1C0C9CC4BD");

    public static SmokeWallpaperBackup Capture()
    {
        var desktopWallpaper = Create();
        SmokeDesktopWallpaperPosition position;
        uint backgroundColor;
        uint count;

        ThrowForHR(desktopWallpaper.GetPosition(out position));
        ThrowForHR(desktopWallpaper.GetBackgroundColor(out backgroundColor));
        ThrowForHR(desktopWallpaper.GetMonitorDevicePathCount(out count));

        var monitors = new List<SmokeWallpaperMonitor>((int)count);
        for (uint index = 0; index < count; index++)
        {
            IntPtr monitorPointer;
            IntPtr wallpaperPointer;

            ThrowForHR(desktopWallpaper.GetMonitorDevicePathAt(index, out monitorPointer));
            var monitorId = StringFromCoTaskMem(monitorPointer);
            ThrowForHR(desktopWallpaper.GetWallpaper(monitorId, out wallpaperPointer));
            monitors.Add(new SmokeWallpaperMonitor
            {
                MonitorId = monitorId,
                WallpaperPath = wallpaperPointer == IntPtr.Zero ? string.Empty : StringFromCoTaskMem(wallpaperPointer),
            });
        }

        return new SmokeWallpaperBackup
        {
            Position = position,
            BackgroundColor = backgroundColor,
            Monitors = monitors.ToArray(),
        };
    }

    public static void Restore(SmokeWallpaperBackup backup)
    {
        if (backup == null)
        {
            throw new ArgumentNullException("backup");
        }

        var desktopWallpaper = Create();
        foreach (var monitor in backup.Monitors)
        {
            if (monitor == null || string.IsNullOrWhiteSpace(monitor.MonitorId) || string.IsNullOrWhiteSpace(monitor.WallpaperPath))
            {
                continue;
            }

            ThrowForHR(desktopWallpaper.SetWallpaper(monitor.MonitorId, monitor.WallpaperPath));
        }

        ThrowForHR(desktopWallpaper.SetPosition(backup.Position));
        ThrowForHR(desktopWallpaper.SetBackgroundColor(backup.BackgroundColor));
    }

    private static ISmokeDesktopWallpaper Create()
    {
        var type = Type.GetTypeFromCLSID(DesktopWallpaperClassId);
        if (type == null)
        {
            throw new InvalidOperationException("IDesktopWallpaper COM class is not registered.");
        }

        return (ISmokeDesktopWallpaper)Activator.CreateInstance(type);
    }

    private static void ThrowForHR(int hresult)
    {
        Marshal.ThrowExceptionForHR(hresult);
    }

    private static string StringFromCoTaskMem(IntPtr pointer)
    {
        try
        {
            return Marshal.PtrToStringUni(pointer) ?? string.Empty;
        }
        finally
        {
            if (pointer != IntPtr.Zero)
            {
                Marshal.FreeCoTaskMem(pointer);
            }
        }
    }
}
"@

function Assert-LastExitCode {
    param([string]$Step)

    if ($null -ne $LASTEXITCODE -and $LASTEXITCODE -ne 0) {
        throw "$Step failed with exit code $LASTEXITCODE."
    }
}

function Stop-LaunchedApp {
    param([int]$ProcessId)

    $process = Get-Process -Id $ProcessId -ErrorAction SilentlyContinue
    if (-not $process) {
        return
    }

    $null = $process.CloseMainWindow()
    Start-Sleep -Seconds 1
    $process = Get-Process -Id $ProcessId -ErrorAction SilentlyContinue
    if ($process) {
        Stop-Process -Id $ProcessId -Force
    }
}

function Find-WallerElementByAutomationId {
    param(
        [System.Windows.Automation.AutomationElement]$Root,
        [string]$AutomationId
    )

    $condition = [System.Windows.Automation.PropertyCondition]::new(
        [System.Windows.Automation.AutomationElement]::AutomationIdProperty,
        $AutomationId)

    return $Root.FindFirst(
        [System.Windows.Automation.TreeScope]::Descendants,
        $condition)
}

function Wait-WallerElementByAutomationId {
    param(
        [System.Windows.Automation.AutomationElement]$Root,
        [string]$AutomationId,
        [int]$TimeoutSeconds = 5
    )

    $deadline = (Get-Date).AddSeconds($TimeoutSeconds)
    while ((Get-Date) -lt $deadline) {
        $element = Find-WallerElementByAutomationId -Root $Root -AutomationId $AutomationId
        if ($element) {
            return $element
        }

        Start-Sleep -Milliseconds 150
    }

    throw "UI Automation element not found: $AutomationId."
}

function Invoke-WallerElementByAutomationId {
    param(
        [System.Windows.Automation.AutomationElement]$Root,
        [string]$AutomationId
    )

    $element = Wait-WallerElementByAutomationId -Root $Root -AutomationId $AutomationId
    if (-not $element.Current.IsEnabled) {
        throw "UI Automation element is disabled: $AutomationId."
    }

    $pattern = $null
    if (-not $element.TryGetCurrentPattern(
        [System.Windows.Automation.InvokePattern]::Pattern,
        [ref]$pattern)) {
        throw "UI Automation element does not support InvokePattern: $AutomationId."
    }

    $pattern.Invoke()
}

function Get-WallerElementNameByAutomationId {
    param(
        [System.Windows.Automation.AutomationElement]$Root,
        [string]$AutomationId
    )

    $element = Wait-WallerElementByAutomationId -Root $Root -AutomationId $AutomationId
    return $element.Current.Name
}

function Set-WallerLocalDataRootFromLaunch {
    param([string]$Aumid)

    $userProfile = [Environment]::GetFolderPath("UserProfile")
    if (-not [string]::IsNullOrWhiteSpace($userProfile)) {
        $script:localDataRoot = Join-Path $userProfile "AppData\Local\Waller"
        return
    }

    $localApplicationData = [Environment]::GetEnvironmentVariable("LOCALAPPDATA")
    if ([string]::IsNullOrWhiteSpace($localApplicationData)) {
        $localApplicationData = [Environment]::GetFolderPath("LocalApplicationData")
    }

    $script:localDataRoot = Join-Path $localApplicationData "Waller"
}

function Assert-RestorableWallpaperBackup {
    param([SmokeWallpaperBackup]$Backup)

    if (-not $Backup -or -not $Backup.Monitors -or $Backup.Monitors.Count -lt 1) {
        throw "Apply smoke is unsafe: no monitors were captured for wallpaper restore."
    }

    foreach ($monitor in $Backup.Monitors) {
        if ([string]::IsNullOrWhiteSpace($monitor.MonitorId)) {
            throw "Apply smoke is unsafe: captured monitor id is blank."
        }

        if ([string]::IsNullOrWhiteSpace($monitor.WallpaperPath)) {
            throw "Apply smoke is unsafe: monitor '$($monitor.MonitorId)' has no wallpaper path to restore."
        }

        if (-not (Test-Path -LiteralPath $monitor.WallpaperPath)) {
            throw "Apply smoke is unsafe: restore wallpaper path does not exist: $($monitor.WallpaperPath)"
        }
    }
}

function Wait-WallerRenderedFiles {
    param(
        [DateTime]$Since,
        [int]$TimeoutSeconds
    )

    $renderedRoot = Join-Path $localDataRoot "rendered"
    $deadline = (Get-Date).AddSeconds($TimeoutSeconds)
    while ((Get-Date) -lt $deadline) {
        if (Test-Path -LiteralPath $renderedRoot) {
            $files = Get-ChildItem -LiteralPath $renderedRoot -Filter *.png -File -ErrorAction SilentlyContinue |
                Where-Object { $_.LastWriteTime -ge $Since }
            if ($files.Count -gt 0) {
                return $files
            }
        }

        Start-Sleep -Milliseconds 250
    }

    throw "Apply smoke did not observe rendered PNG output under $renderedRoot."
}

function Wait-WallerApplySuccess {
    param(
        [System.Windows.Automation.AutomationElement]$Root,
        [int]$TimeoutSeconds
    )

    $deadline = (Get-Date).AddSeconds($TimeoutSeconds)
    $lastStatus = ""
    while ((Get-Date) -lt $deadline) {
        $lastStatus = Get-WallerElementNameByAutomationId -Root $Root -AutomationId "StatusInfoBar"
        if ($lastStatus -match "(?i)(Apply finished|Aplicar terminado)" -and
            $lastStatus -match "(?i)(0 failed|0 fallaron)") {
            return $lastStatus
        }

        if ($lastStatus -match "(?i)(failed|fallaron)" -and
            $lastStatus -notmatch "(?i)(0 failed|0 fallaron)") {
            throw "Apply smoke observed failed Apply status: $lastStatus"
        }

        Start-Sleep -Milliseconds 250
    }

    throw "Apply smoke did not observe successful Apply status. Last status: $lastStatus"
}

function Count-WallerRenderedWallpaperPaths {
    param([SmokeWallpaperBackup]$Backup)

    $backupPaths = @{}
    foreach ($monitor in $Backup.Monitors) {
        $backupPaths[$monitor.MonitorId] = $monitor.WallpaperPath
    }

    $current = [SmokeDesktopWallpaperInterop]::Capture()
    $count = 0
    foreach ($monitor in $current.Monitors) {
        if ($monitor.WallpaperPath -match "\\Waller\\rendered\\" -and
            $backupPaths.ContainsKey($monitor.MonitorId) -and
            -not [string]::Equals($backupPaths[$monitor.MonitorId], $monitor.WallpaperPath, [StringComparison]::OrdinalIgnoreCase)) {
            $count++
        }
    }

    return $count
}

function Assert-WallpaperRestored {
    param([SmokeWallpaperBackup]$Backup)

    $current = [SmokeDesktopWallpaperInterop]::Capture()
    foreach ($expected in $Backup.Monitors) {
        $actual = $current.Monitors | Where-Object { $_.MonitorId -eq $expected.MonitorId } | Select-Object -First 1
        if (-not $actual) {
            throw "Wallpaper restore verification failed: missing monitor '$($expected.MonitorId)'."
        }

        if (-not [string]::Equals($expected.WallpaperPath, $actual.WallpaperPath, [StringComparison]::OrdinalIgnoreCase)) {
            throw "Wallpaper restore verification failed for '$($expected.MonitorId)'. Expected '$($expected.WallpaperPath)', got '$($actual.WallpaperPath)'."
        }
    }
}

Push-Location $nativeRoot
try {
    Add-Type -AssemblyName UIAutomationClient
    Add-Type -AssemblyName UIAutomationTypes

    $wallpaperBackup = [SmokeDesktopWallpaperInterop]::Capture()
    Assert-RestorableWallpaperBackup -Backup $wallpaperBackup

    $buildArgs = @($ProjectPath, "-Detach")
    if ($DisableNuGetAudit) {
        $buildArgs += "-DisableNuGetAudit"
    }

    $output = powershell -ExecutionPolicy Bypass -File $buildScript @buildArgs 2>&1
    Assert-LastExitCode "Packaged launch"
    $text = $output | Out-String
    Write-Host $text

    $jsonMatch = [regex]::Match($text, "(?s)\{.*\}\s*$")
    if (-not $jsonMatch.Success) {
        throw "Launch output did not include trailing winapp JSON."
    }

    $launch = $jsonMatch.Value | ConvertFrom-Json
    if ($launch.Error) {
        throw "winapp launch failed: $($launch.Error)"
    }

    Set-WallerLocalDataRootFromLaunch -Aumid $launch.AUMID

    if (-not $launch.ProcessId) {
        throw "Launch JSON did not include ProcessId."
    }

    $appProcessId = [int]$launch.ProcessId
    $deadline = (Get-Date).AddSeconds($LaunchTimeoutSeconds)
    $process = $null
    $window = $null

    while ((Get-Date) -lt $deadline) {
        $process = Get-Process -Id $appProcessId -ErrorAction SilentlyContinue
        if ($process -and $process.MainWindowTitle) {
            $processCondition = [System.Windows.Automation.PropertyCondition]::new(
                [System.Windows.Automation.AutomationElement]::ProcessIdProperty,
                $appProcessId)
            $window = [System.Windows.Automation.AutomationElement]::RootElement.FindFirst(
                [System.Windows.Automation.TreeScope]::Children,
                $processCondition)
            if ($window) {
                break
            }
        }

        Start-Sleep -Milliseconds 250
    }

    if (-not $process) {
        throw "Launched process $appProcessId was not found."
    }

    if ($process.ProcessName -ne "Waller.Native.App") {
        throw "Unexpected process name: $($process.ProcessName)."
    }

    if ($process.MainWindowTitle -ne "Waller") {
        throw "Unexpected main window title: $($process.MainWindowTitle)."
    }

    if (-not $process.Responding) {
        throw "Launched app is not responding."
    }

    if (-not $window) {
        throw "UI Automation window not found for launched process $appProcessId."
    }

    $applyStartedAt = Get-Date
    Invoke-WallerElementByAutomationId -Root $window -AutomationId "ApplyAllButton"
    $status = Wait-WallerApplySuccess -Root $window -TimeoutSeconds $ApplyTimeoutSeconds
    $renderedFiles = Wait-WallerRenderedFiles -Since $applyStartedAt -TimeoutSeconds $ApplyTimeoutSeconds
    $changedWallpaperPaths = Count-WallerRenderedWallpaperPaths -Backup $wallpaperBackup

    [pscustomobject]@{
        ProcessId = $appProcessId
        Status = $status
        RenderedFiles = $renderedFiles.Count
        WallpaperPathsChanged = $changedWallpaperPaths
        RestorableMonitors = $wallpaperBackup.Monitors.Count
    } | Format-List | Out-String | Write-Host

    Stop-LaunchedApp -ProcessId $appProcessId
    [SmokeDesktopWallpaperInterop]::Restore($wallpaperBackup)
    Assert-WallpaperRestored -Backup $wallpaperBackup

    Write-Host "SMOKE APPLY PASSED: $appProcessId"
}
finally {
    if ($appProcessId) {
        Stop-LaunchedApp -ProcessId $appProcessId
    }

    if ($wallpaperBackup) {
        [SmokeDesktopWallpaperInterop]::Restore($wallpaperBackup)
    }

    Pop-Location
}
