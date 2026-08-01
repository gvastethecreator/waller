using Microsoft.UI.Xaml;

namespace Waller.Native.App.ViewModels;

public sealed partial class MainPageViewModel
{
    public LocalizedText Text => LocalizedText.For(SelectedLanguage);

    public bool CanStartApply => workspace.CanStartApply;

    public bool CanEditSession => workspace.CanEditSession;

    public bool CanUseShellCommands => workspace.CanUseShellCommands;

    public string SessionSummary => Text.SessionSummary(
        activeSession.BasedOnPreset,
        activeSession.HasUnsavedPresetChanges,
        activeSession.MissingAssignments.Count,
        Presets.SelectedPreset);
}
