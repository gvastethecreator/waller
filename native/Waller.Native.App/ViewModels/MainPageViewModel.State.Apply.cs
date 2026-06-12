using CommunityToolkit.Mvvm.ComponentModel;

namespace Waller.Native.App.ViewModels;

public sealed partial class MainPageViewModel
{
    [ObservableProperty]
    public partial string StatusText { get; set; } = LocalizedText.English.LoadedCurrentSetup;

    [ObservableProperty]
    public partial bool IsApplying { get; set; }

    [ObservableProperty]
    public partial string ApplyProgressText { get; set; } = string.Empty;
}
