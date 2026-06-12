using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Waller.Native.App.ViewModels;

public sealed partial class MainPageViewModel
{
    public ObservableCollection<PresetMenuItem> Presets { get; } = [];

    public ObservableCollection<PresetMenuItem> ManagePresetItems { get; } = [];

    [ObservableProperty]
    public partial PresetMenuItem? SelectedPreset { get; set; }

    [ObservableProperty]
    public partial string PresetNameDraft { get; set; } = string.Empty;

    [ObservableProperty]
    public partial PresetMenuItem? SelectedManagePreset { get; set; }

    [ObservableProperty]
    public partial string ManagePresetNameDraft { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string SaveAsPresetNameDraft { get; set; } = string.Empty;
}
