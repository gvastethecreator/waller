using CommunityToolkit.Mvvm.ComponentModel;

namespace Waller.Native.App.ViewModels;

public sealed partial class MainPageViewModel
{
    [ObservableProperty]
    public partial string StatusText { get; set; } = LocalizedText.English.LoadedCurrentSetup;
}
