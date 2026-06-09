namespace Waller.Native.App.ViewModels;

internal readonly record struct ShellInteractionState(
    bool IsApplying,
    bool IsSaveAsOpen,
    bool IsManagePresetsOpen,
    bool IsDeleteConfirmationOpen,
    bool IsSettingsOpen)
{
    public bool IsAnyModalOpen =>
        IsSaveAsOpen || IsManagePresetsOpen || IsDeleteConfirmationOpen || IsSettingsOpen;

    public ShellModalLayer TopModal =>
        IsDeleteConfirmationOpen ? ShellModalLayer.DeleteConfirmation
        : IsManagePresetsOpen ? ShellModalLayer.ManagePresets
        : IsSaveAsOpen ? ShellModalLayer.SaveAs
        : IsSettingsOpen ? ShellModalLayer.Settings
        : ShellModalLayer.None;

    public bool CanStartApply => !IsApplying && !IsAnyModalOpen;

    public bool CanEditSession => !IsApplying;

    public bool CanEditMonitorAssignment => CanEditSession && !IsAnyModalOpen;

    public bool CanUseShellCommands => !IsApplying && !IsAnyModalOpen;

    public bool CanMutateManagedPresets => !IsApplying && !IsDeleteConfirmationOpen;

    public bool CanUseModalActions => !IsApplying;
}
