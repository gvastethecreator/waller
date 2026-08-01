using Waller.Native.Core.Models;
using Waller.Native.Workflows.Shell;

namespace Waller.Native.Tests.Workflows;

public sealed class ShellWorkspaceTests
{
    [Fact]
    public void ReplaceActiveSession_ExposesReplacement()
    {
        var workspace = CreateWorkspace();
        var replacement = ActiveSession.FromMonitors([]);

        workspace.ReplaceActiveSession(replacement);

        Assert.Same(replacement, workspace.ActiveSession);
    }

    [Fact]
    public void ModalStack_RejectsInvalidCombinations()
    {
        var workspace = CreateWorkspace();

        Assert.False(workspace.TryOpenModal(ShellModal.DeleteConfirmation));
        Assert.True(workspace.TryOpenModal(ShellModal.ManagePresets));
        Assert.False(workspace.TryOpenModal(ShellModal.Settings));
        Assert.True(workspace.TryOpenModal(ShellModal.DeleteConfirmation));
        Assert.False(workspace.TryOpenModal(ShellModal.DeleteConfirmation));

        Assert.Equal(
            [ShellModal.ManagePresets, ShellModal.DeleteConfirmation],
            workspace.ModalStack);
    }

    [Fact]
    public void CloseTopModal_LeavesManagePresetsParentOpen()
    {
        var workspace = CreateWorkspace();
        workspace.TryOpenModal(ShellModal.ManagePresets);
        workspace.TryOpenModal(ShellModal.DeleteConfirmation);

        var closed = workspace.TryCloseTopModal(out var closedModal);

        Assert.True(closed);
        Assert.Equal(ShellModal.DeleteConfirmation, closedModal);
        Assert.Equal(ShellModal.ManagePresets, workspace.TopModal);
        Assert.True(workspace.IsModalOpen(ShellModal.ManagePresets));
        Assert.False(workspace.IsModalOpen(ShellModal.DeleteConfirmation));
    }

    [Fact]
    public void CloseTopModal_ReturnsFalseForEmptyStack()
    {
        var workspace = CreateWorkspace();

        Assert.False(workspace.TryCloseTopModal(out _));
        Assert.Null(workspace.TopModal);
    }

    [Fact]
    public void ApplyLease_IsExclusiveAndReleasesOnDispose()
    {
        var workspace = CreateWorkspace();
        using var firstLease = workspace.BeginApply();

        Assert.True(workspace.IsApplyActive);
        Assert.Throws<InvalidOperationException>(() => workspace.BeginApply());

        firstLease.Dispose();
        using var secondLease = workspace.BeginApply();

        Assert.True(workspace.IsApplyActive);
    }

    [Fact]
    public void ApplyLease_CancelRequestsItsToken()
    {
        var workspace = CreateWorkspace();
        using var lease = workspace.BeginApply();

        lease.Cancel();

        Assert.True(lease.IsCancellationRequested);
        Assert.True(lease.Token.IsCancellationRequested);
    }

    [Fact]
    public void BeginApply_RejectsOpenModal()
    {
        var workspace = CreateWorkspace();
        workspace.TryOpenModal(ShellModal.Settings);

        Assert.Throws<InvalidOperationException>(() => workspace.BeginApply());
    }

    [Fact]
    public void Permissions_FollowModalAndApplyTransitions()
    {
        var workspace = CreateWorkspace();

        Assert.True(workspace.CanStartApply);
        Assert.True(workspace.CanEditMonitorAssignment);
        Assert.True(workspace.CanUseShellCommands);

        workspace.TryOpenModal(ShellModal.ManagePresets);
        Assert.False(workspace.CanStartApply);
        Assert.True(workspace.CanEditSession);
        Assert.False(workspace.CanEditMonitorAssignment);
        Assert.True(workspace.CanMutateManagedPresets);

        workspace.TryOpenModal(ShellModal.DeleteConfirmation);
        Assert.False(workspace.CanMutateManagedPresets);
        Assert.True(workspace.CanUseModalActions);

        workspace.TryCloseTopModal(out _);
        workspace.TryCloseTopModal(out _);
        using var lease = workspace.BeginApply();
        Assert.False(workspace.CanEditSession);
        Assert.False(workspace.CanUseModalActions);
        Assert.False(workspace.TryOpenModal(ShellModal.SaveAs));
    }

    private static ShellWorkspace CreateWorkspace() =>
        new(ActiveSession.FromMonitors([]));
}
