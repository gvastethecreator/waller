using Waller.Native.Core.Models;

namespace Waller.Native.Workflows.Shell;

public sealed class ShellWorkspace : IShellWorkspace
{
    private readonly object gate = new();
    private readonly List<ShellModal> modalStack = [];
    private ActiveSession activeSession;
    private ApplyLease? activeApplyLease;

    public ShellWorkspace(ActiveSession activeSession)
    {
        ArgumentNullException.ThrowIfNull(activeSession);
        this.activeSession = activeSession;
    }

    public ActiveSession ActiveSession
    {
        get
        {
            lock (gate)
            {
                return activeSession;
            }
        }
    }

    public IReadOnlyList<ShellModal> ModalStack
    {
        get
        {
            lock (gate)
            {
                return modalStack.ToArray();
            }
        }
    }

    public ShellModal? TopModal
    {
        get
        {
            lock (gate)
            {
                return modalStack.Count == 0 ? null : modalStack[^1];
            }
        }
    }

    public bool IsApplyActive
    {
        get
        {
            lock (gate)
            {
                return activeApplyLease is not null;
            }
        }
    }

    public bool IsAnyModalOpen
    {
        get
        {
            lock (gate)
            {
                return modalStack.Count > 0;
            }
        }
    }

    public bool CanStartApply => !IsApplyActive && !IsAnyModalOpen;

    public bool CanEditSession => !IsApplyActive;

    public bool CanEditMonitorAssignment => CanEditSession && !IsAnyModalOpen;

    public bool CanUseShellCommands => !IsApplyActive && !IsAnyModalOpen;

    public bool CanMutateManagedPresets => !IsApplyActive && TopModal != ShellModal.DeleteConfirmation;

    public bool CanUseModalActions => !IsApplyActive;

    public void ReplaceActiveSession(ActiveSession session)
    {
        ArgumentNullException.ThrowIfNull(session);
        lock (gate)
        {
            activeSession = session;
        }
    }

    public bool IsModalOpen(ShellModal modal)
    {
        lock (gate)
        {
            return modalStack.Contains(modal);
        }
    }

    public bool TryOpenModal(ShellModal modal)
    {
        lock (gate)
        {
            if (activeApplyLease is not null)
            {
                return false;
            }

            if (modal == ShellModal.DeleteConfirmation)
            {
                if (modalStack.Count != 1 || modalStack[0] != ShellModal.ManagePresets)
                {
                    return false;
                }

                modalStack.Add(modal);
                return true;
            }

            if (modalStack.Count != 0)
            {
                return false;
            }

            modalStack.Add(modal);
            return true;
        }
    }

    public bool TryCloseTopModal(out ShellModal closedModal)
    {
        lock (gate)
        {
            if (modalStack.Count == 0)
            {
                closedModal = default;
                return false;
            }

            closedModal = modalStack[^1];
            modalStack.RemoveAt(modalStack.Count - 1);
            return true;
        }
    }

    public ApplyLease BeginApply()
    {
        lock (gate)
        {
            if (activeApplyLease is not null)
            {
                throw new InvalidOperationException("An Apply lease is already active.");
            }

            if (modalStack.Count != 0)
            {
                throw new InvalidOperationException("Apply cannot start while a modal is open.");
            }

            activeApplyLease = new ApplyLease(this);
            return activeApplyLease;
        }
    }

    internal void ReleaseApply(ApplyLease lease)
    {
        ArgumentNullException.ThrowIfNull(lease);
        lock (gate)
        {
            if (!ReferenceEquals(activeApplyLease, lease))
            {
                throw new InvalidOperationException("The Apply lease does not belong to this workspace.");
            }

            activeApplyLease = null;
        }
    }
}
