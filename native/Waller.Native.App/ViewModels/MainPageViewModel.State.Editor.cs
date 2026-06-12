using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using Waller.Native.Core.Models;
using Windows.UI;

namespace Waller.Native.App.ViewModels;

public sealed partial class MainPageViewModel
{
    public ObservableCollection<OptionItem<WallpaperSourceKind>> SourceOptions { get; } = [];

    public ObservableCollection<OptionItem<WallpaperFitMode>> FitOptions { get; } = [];

    public ObservableCollection<OptionItem<WallpaperAnchor>> AnchorOptions { get; } = [];

    public ObservableCollection<ColorSwatchOption> ColorSwatches { get; } =
        new(ColorSwatchCatalog.Defaults());

    [ObservableProperty]
    public partial MonitorRowViewModel? SelectedMonitor { get; set; }

    [ObservableProperty]
    public partial OptionItem<WallpaperSourceKind>? SelectedSourceOption { get; set; }

    [ObservableProperty]
    public partial OptionItem<WallpaperFitMode>? SelectedFitOption { get; set; }

    [ObservableProperty]
    public partial OptionItem<WallpaperAnchor>? SelectedAnchorOption { get; set; }

    [ObservableProperty]
    public partial WallpaperSourceKind EditSourceKind { get; set; } = WallpaperSourceKind.Empty;

    [ObservableProperty]
    public partial string EditImagePath { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string EditColorHex { get; set; } = "#000000";

    [ObservableProperty]
    public partial Color EditColor { get; set; } = Color.FromArgb(255, 0, 0, 0);

    [ObservableProperty]
    public partial WallpaperFitMode EditFitMode { get; set; } = WallpaperFitMode.Cover;

    [ObservableProperty]
    public partial WallpaperAnchor EditAnchor { get; set; } = WallpaperAnchor.Center;

    [ObservableProperty]
    public partial double EditOffsetXPercent { get; set; }

    [ObservableProperty]
    public partial double EditOffsetYPercent { get; set; }
}
