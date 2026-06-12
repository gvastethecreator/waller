using Microsoft.UI.Xaml;

namespace Waller.Native.App.ViewModels;

public sealed partial class MainPageViewModel
{
    public bool CanMutateManagedPresets => InteractionState.CanMutateManagedPresets;

    public bool CanUseModalActions => InteractionState.CanUseModalActions;

    public bool IsAnyModalOpen => InteractionState.IsAnyModalOpen;

    public Visibility ManagePresetsVisibility => VisibilityStates.When(IsManagePresetsOpen);

    public Visibility SaveAsVisibility => VisibilityStates.When(IsSaveAsOpen);

    public Visibility DeleteConfirmationVisibility => VisibilityStates.When(IsDeleteConfirmationOpen);

    public string DeleteConfirmationMessage =>
        pendingDeletePreset?.Message(Text) ?? Text.DeleteSelectedPreset;

    public Visibility SettingsVisibility => VisibilityStates.When(IsSettingsOpen);
}
