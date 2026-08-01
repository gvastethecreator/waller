using Microsoft.UI.Xaml;

namespace Waller.Native.App.ViewModels;

public sealed partial class MainPageViewModel
{
    public bool CanUseModalActions => workspace.CanUseModalActions;

    public bool IsAnyModalOpen => workspace.IsAnyModalOpen;

    public Visibility SettingsVisibility => VisibilityStates.When(IsSettingsOpen);
}
