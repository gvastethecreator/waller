using Waller.Native.Core.Models;

namespace Waller.Native.Workflows.Shell;

public interface IShellWorkspace
{
    ActiveSession ActiveSession { get; }

    IReadOnlyList<ShellModal> ModalStack { get; }

    ShellModal? TopModal { get; }

    bool IsApplyActive { get; }

    bool IsAnyModalOpen { get; }

    bool CanStartApply { get; }

    bool CanEditSession { get; }

    bool CanEditMonitorAssignment { get; }

    bool CanUseShellCommands { get; }

    bool CanMutateManagedPresets { get; }

    bool CanUseModalActions { get; }

    void ReplaceActiveSession(ActiveSession session);

    bool IsModalOpen(ShellModal modal);

    bool TryOpenModal(ShellModal modal);

    bool TryCloseTopModal(out ShellModal closedModal);

    ApplyLease BeginApply();
}
