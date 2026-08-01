using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Xaml;
using Waller.Native.Core.Models;
using Waller.Native.Workflows.Presets;
using Waller.Native.Workflows.Settings;
using Waller.Native.Workflows.Shell;

namespace Waller.Native.App.ViewModels;

public sealed partial class PresetsViewModel : ObservableObject
{
    private readonly PresetWorkflow workflow;
    private readonly UserSettingsWorkflow userSettings;
    private readonly IShellWorkspace workspace;
    private readonly Func<LocalizedText> text;
    private readonly Action<string> reportStatus;
    private readonly Action<bool> refreshSessionSurface;
    private readonly Action notifySessionSummary;
    private readonly Action notifyWorkspaceState;
    private readonly PresetTextPresenter presetText;
    private readonly ShellStatusTextPresenter shellText;
    private Preset? selectedPresetRecord;
    private PresetDeleteConfirmation? pendingDeletePreset;
    private bool isChangingSelection;
    private int selectionVersion;
    private Task selectionTask = Task.CompletedTask;

    internal PresetsViewModel(
        PresetWorkflow workflow,
        UserSettingsWorkflow userSettings,
        IShellWorkspace workspace,
        Func<LocalizedText> text,
        Action<string> reportStatus,
        Action<bool> refreshSessionSurface,
        Action notifySessionSummary,
        Action notifyWorkspaceState)
    {
        ArgumentNullException.ThrowIfNull(workflow);
        ArgumentNullException.ThrowIfNull(userSettings);
        ArgumentNullException.ThrowIfNull(workspace);
        ArgumentNullException.ThrowIfNull(text);
        ArgumentNullException.ThrowIfNull(reportStatus);
        ArgumentNullException.ThrowIfNull(refreshSessionSurface);
        ArgumentNullException.ThrowIfNull(notifySessionSummary);
        ArgumentNullException.ThrowIfNull(notifyWorkspaceState);

        this.workflow = workflow;
        this.userSettings = userSettings;
        this.workspace = workspace;
        this.text = LocalizedTextSource.Require(text);
        this.reportStatus = reportStatus;
        this.refreshSessionSurface = refreshSessionSurface;
        this.notifySessionSummary = notifySessionSummary;
        this.notifyWorkspaceState = notifyWorkspaceState;
        presetText = new PresetTextPresenter(this.text);
        shellText = new ShellStatusTextPresenter(this.text);
    }

    public ObservableCollection<PresetMenuItem> Items { get; } = [];

    public ObservableCollection<PresetMenuItem> ManagedItems { get; } = [];

    [ObservableProperty]
    public partial PresetMenuItem? SelectedPreset { get; set; }

    [ObservableProperty]
    public partial string PresetNameDraft { get; set; } = string.Empty;

    [ObservableProperty]
    public partial PresetMenuItem? SelectedManagedPreset { get; set; }

    [ObservableProperty]
    public partial string ManagedPresetNameDraft { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string SaveAsPresetNameDraft { get; set; } = string.Empty;

    public LocalizedText Text => text();

    public Guid? LastSelectedPresetId { get; private set; }

    public bool CanUseShellCommands => workspace.CanUseShellCommands;

    public bool CanMutateManagedPresets => workspace.CanMutateManagedPresets;

    public bool CanUseModalActions => workspace.CanUseModalActions;

    public bool IsSaveAsOpen => workspace.IsModalOpen(ShellModal.SaveAs);

    public bool IsManagePresetsOpen => workspace.IsModalOpen(ShellModal.ManagePresets);

    public bool IsDeleteConfirmationOpen => workspace.IsModalOpen(ShellModal.DeleteConfirmation);

    public Visibility SaveAsVisibility => VisibilityStates.When(IsSaveAsOpen);

    public Visibility ManagePresetsVisibility => VisibilityStates.When(IsManagePresetsOpen);

    public Visibility DeleteConfirmationVisibility => VisibilityStates.When(IsDeleteConfirmationOpen);

    public Visibility ManagedPresetEmptyVisibility =>
        VisibilityStates.When(ManagedItems.Count == 0);

    public string DeleteConfirmationMessage =>
        pendingDeletePreset?.Message(Text) ?? Text.DeleteSelectedPreset;

    internal void SetLastSelectedPresetId(Guid? presetId)
    {
        LastSelectedPresetId = PresetIds.NormalizeOptional(presetId);
    }

    internal void NotifyWorkspaceStateChanged()
    {
        OnPropertyChanged(nameof(CanUseShellCommands));
        OnPropertyChanged(nameof(CanMutateManagedPresets));
        OnPropertyChanged(nameof(CanUseModalActions));
        OnPropertyChanged(nameof(IsSaveAsOpen));
        OnPropertyChanged(nameof(IsManagePresetsOpen));
        OnPropertyChanged(nameof(IsDeleteConfirmationOpen));
        OnPropertyChanged(nameof(SaveAsVisibility));
        OnPropertyChanged(nameof(ManagePresetsVisibility));
        OnPropertyChanged(nameof(DeleteConfirmationVisibility));
    }

    internal void RefreshLocalizedSurface()
    {
        isChangingSelection = true;
        try
        {
            SelectedPreset = PresetMenuLists.ReplaceCurrentSetupName(
                Items,
                SelectedPreset,
                Text.CurrentSetup);
        }
        finally
        {
            isChangingSelection = false;
        }

        OnPropertyChanged(nameof(Text));
        OnPropertyChanged(nameof(DeleteConfirmationMessage));
        OnPropertyChanged(nameof(ManagedPresetEmptyVisibility));
    }

    internal void CloseTopPresetModal(ShellModal modal)
    {
        switch (modal)
        {
            case ShellModal.DeleteConfirmation:
                ClearPendingDeletePreset();
                break;
            case ShellModal.ManagePresets:
                CloseManagePresets();
                break;
            case ShellModal.SaveAs:
                CloseSaveAs();
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(modal), modal, "Modal is not owned by Presets.");
        }
    }

    private ActiveSession activeSession => workspace.ActiveSession;

    private void SetStatus(string value) => reportStatus(value);

    private bool TryOpenModal(ShellModal modal)
    {
        if (!workspace.TryOpenModal(modal))
        {
            return false;
        }

        NotifyWorkspaceStateChanged();
        notifyWorkspaceState();
        return true;
    }

    private bool TryCloseModal(ShellModal modal)
    {
        if (workspace.TopModal != modal || !workspace.TryCloseTopModal(out _))
        {
            return false;
        }

        NotifyWorkspaceStateChanged();
        notifyWorkspaceState();
        return true;
    }

    private void ClearPendingDeletePreset()
    {
        pendingDeletePreset = null;
        TryCloseModal(ShellModal.DeleteConfirmation);
        OnPropertyChanged(nameof(DeleteConfirmationMessage));
        OnPropertyChanged(nameof(DeleteConfirmationVisibility));
    }

    partial void OnSelectedPresetChanged(PresetMenuItem? value)
    {
        if (value is null || isChangingSelection)
        {
            return;
        }

        var loadVersion = ++selectionVersion;
        selectionTask = LoadSelectedPresetAsync(value, loadVersion);
    }

    partial void OnSelectedManagedPresetChanged(PresetMenuItem? value)
    {
        ManagedPresetNameDraft = ManagedPresetSelection.NameDraft(value);
    }
}
