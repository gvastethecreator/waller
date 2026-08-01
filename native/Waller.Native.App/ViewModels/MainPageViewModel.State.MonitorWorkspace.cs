using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Waller.Native.App.ViewModels;

public sealed partial class MainPageViewModel
{
    public ObservableCollection<MonitorRowViewModel> Monitors { get; } = [];

    public IEnumerable<MonitorRowViewModel> TopologyMonitors =>
        Monitors.OrderBy(monitor => monitor.TopologyLeft);

    public string MonitorCountHeader => $"{Text.Monitors} ({Monitors.Count})";

    public ObservableCollection<MissingMonitorRowViewModel> MissingMonitors { get; } = [];

    [ObservableProperty]
    public partial double TopologyWidth { get; set; } = 960;

    [ObservableProperty]
    public partial double TopologyHeight { get; set; } = 312;
}
