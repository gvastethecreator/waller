using Microsoft.UI.Xaml;

namespace Waller.Native.App.ViewModels;

public sealed partial class MainPageViewModel
{
    public LocalizedText Text => LocalizedText.For(SelectedLanguage);

    public Visibility ApplyProgressVisibility => VisibilityStates.When(IsApplying);

    private ShellInteractionState InteractionState => new(
        IsApplying,
        IsSaveAsOpen,
        IsManagePresetsOpen,
        IsDeleteConfirmationOpen,
        IsSettingsOpen);

    private ApplyTextPresenter applyText => textPresenters.Apply;

    private PresetTextPresenter presetText => textPresenters.Preset;

    private MonitorEditTextPresenter monitorEditText => textPresenters.MonitorEdit;

    private ShellStatusTextPresenter shellText => textPresenters.Shell;

    public bool CanStartApply => InteractionState.CanStartApply;

    public bool CanEditSession => InteractionState.CanEditSession;

    public bool CanUseShellCommands => InteractionState.CanUseShellCommands;

    public string SessionSummary => Text.SessionSummary(
        activeSession.BasedOnPreset,
        activeSession.HasUnsavedPresetChanges,
        activeSession.MissingAssignments.Count,
        SelectedPreset);
}
