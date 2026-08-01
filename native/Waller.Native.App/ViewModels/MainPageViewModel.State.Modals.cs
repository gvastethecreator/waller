using Waller.Native.Workflows.Shell;

namespace Waller.Native.App.ViewModels;

public sealed partial class MainPageViewModel
{
    public bool IsSettingsOpen => workspace.IsModalOpen(ShellModal.Settings);
}
