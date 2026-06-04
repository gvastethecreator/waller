using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;

namespace WallerWinUIPrototype.ViewModels;

public partial class MonitorDraft : ObservableObject
{
    [ObservableProperty]
    public partial string Id { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string Name { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string Geometry { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string ImagePath { get; set; } = "__NONE__";

    [ObservableProperty]
    public partial string FitMode { get; set; } = "Fill";

    [ObservableProperty]
    public partial bool Dirty { get; set; }

    [ObservableProperty]
    public partial string LastAction { get; set; } = "Waiting";
}

public partial class MainPageViewModel : ObservableObject
{
    [ObservableProperty]
    public partial ObservableCollection<MonitorDraft> Monitors { get; set; } = new();

    [ObservableProperty]
    public partial MonitorDraft? SelectedMonitor { get; set; }

    [ObservableProperty]
    public partial string SelectedImagePath { get; set; } = "__NONE__";

    [ObservableProperty]
    public partial string SelectedFitMode { get; set; } = "Fill";

    [ObservableProperty]
    public partial string ProfileName { get; set; } = "Prototype profile";

    [ObservableProperty]
    public partial string StatusText { get; set; } = "Prototype ready. Refresh mock monitors to start.";

    [ObservableProperty]
    public partial string StateDump { get; set; } = string.Empty;

    private int pickCounter;

    public MainPageViewModel()
    {
        RefreshMockMonitors();
    }

    partial void OnSelectedMonitorChanged(MonitorDraft? value)
    {
        if (value is null)
        {
            SelectedImagePath = "__NONE__";
            SelectedFitMode = "Fill";
            return;
        }

        SelectedImagePath = value.ImagePath;
        SelectedFitMode = value.FitMode;
        StatusText = $"Selected {value.Name}.";
        UpdateStateDump();
    }

    partial void OnSelectedImagePathChanged(string value)
    {
        if (SelectedMonitor is null)
        {
            return;
        }

        SelectedMonitor.ImagePath = string.IsNullOrWhiteSpace(value) ? "__NONE__" : value.Trim();
        SelectedMonitor.Dirty = true;
        SelectedMonitor.LastAction = "Draft changed";
        UpdateStateDump();
    }

    partial void OnSelectedFitModeChanged(string value)
    {
        if (SelectedMonitor is null)
        {
            return;
        }

        SelectedMonitor.FitMode = string.IsNullOrWhiteSpace(value) ? "Fill" : value.Trim();
        SelectedMonitor.Dirty = true;
        SelectedMonitor.LastAction = "Fit changed";
        UpdateStateDump();
    }

    [RelayCommand]
    private void RefreshMockMonitors()
    {
        Monitors.Clear();
        Monitors.Add(new MonitorDraft
        {
            Id = @"\\?\DISPLAY#WALLER-1",
            Name = "Monitor 1",
            Geometry = "2560 x 1440 @ 0,0",
            ImagePath = @"C:\Wallpapers\studio.png",
            FitMode = "Fill",
            LastAction = "Detected",
        });
        Monitors.Add(new MonitorDraft
        {
            Id = @"\\?\DISPLAY#WALLER-2",
            Name = "Monitor 2",
            Geometry = "1920 x 1080 @ 2560,120",
            ImagePath = "__SOLID__:#1f6feb",
            FitMode = "Fit",
            LastAction = "Detected",
        });
        Monitors.Add(new MonitorDraft
        {
            Id = @"\\?\DISPLAY#WALLER-3",
            Name = "Monitor 3",
            Geometry = "1280 x 1024 @ -1280,200",
            ImagePath = "__NONE__",
            FitMode = "Center",
            LastAction = "Detected",
        });

        SelectedMonitor = Monitors[0];
        StatusText = "Loaded mock monitors. Native monitor detection is the next slice.";
        UpdateStateDump();
    }

    [RelayCommand]
    private void PickMockImage()
    {
        if (SelectedMonitor is null)
        {
            StatusText = "Select a monitor first.";
            return;
        }

        pickCounter++;
        SelectedImagePath = $@"C:\Wallpapers\prototype-{pickCounter}.png";
        StatusText = "Mock image selected. Replace this with FileOpenPicker in the native slice.";
    }

    [RelayCommand]
    private void ClearSelected()
    {
        if (SelectedMonitor is null)
        {
            StatusText = "Select a monitor first.";
            return;
        }

        SelectedImagePath = "__NONE__";
        StatusText = "Selected monitor cleared.";
    }

    [RelayCommand]
    private void ApplySelected()
    {
        if (SelectedMonitor is null)
        {
            StatusText = "Select a monitor first.";
            return;
        }

        SelectedMonitor.Dirty = false;
        SelectedMonitor.LastAction = $"Would apply {SelectedMonitor.FitMode}";
        StatusText = $"Prototype apply: {SelectedMonitor.Name}. Port IDesktopWallpaper here.";
        UpdateStateDump();
    }

    [RelayCommand]
    private void ApplyAll()
    {
        foreach (var monitor in Monitors)
        {
            monitor.Dirty = false;
            monitor.LastAction = $"Would apply {monitor.FitMode}";
        }

        StatusText = "Prototype apply-all complete. No OS wallpaper was changed.";
        UpdateStateDump();
    }

    [RelayCommand]
    private void SaveDraftProfile()
    {
        StatusText = $"Saved '{ProfileName}' in memory. Real persistence is intentionally skipped.";
        UpdateStateDump();
    }

    private void UpdateStateDump()
    {
        StateDump = string.Join(
            Environment.NewLine,
            Monitors.Select(monitor =>
                $"{monitor.Name} | {monitor.Geometry} | fit={monitor.FitMode} | dirty={monitor.Dirty} | source={monitor.ImagePath} | {monitor.LastAction}"));
    }
}
