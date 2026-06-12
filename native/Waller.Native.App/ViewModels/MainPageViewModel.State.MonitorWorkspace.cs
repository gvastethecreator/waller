using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Waller.Native.App.ViewModels;

public sealed partial class MainPageViewModel
{
    public ObservableCollection<MonitorRowViewModel> Monitors { get; } = [];

    public ObservableCollection<MissingMonitorRowViewModel> MissingMonitors { get; } = [];

    [ObservableProperty]
    public partial double TopologyWidth { get; set; } = 720;

    [ObservableProperty]
    public partial double TopologyHeight { get; set; } = 96;
}
