using Microsoft.UI.Xaml;

namespace Waller.Native.App.ViewModels;

public sealed partial class MainPageViewModel
{
    public Visibility ManagePresetEmptyVisibility =>
        VisibilityStates.When(ManagePresetItems.Count == 0);
}
