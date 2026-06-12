using CommunityToolkit.Mvvm.ComponentModel;

namespace Waller.Native.App.ViewModels;

public sealed partial class MainPageViewModel
{
    [ObservableProperty]
    public partial bool IsSaveAsOpen { get; set; }

    [ObservableProperty]
    public partial bool IsManagePresetsOpen { get; set; }

    [ObservableProperty]
    public partial bool IsDeleteConfirmationOpen { get; set; }

    [ObservableProperty]
    public partial bool IsSettingsOpen { get; set; }
}
