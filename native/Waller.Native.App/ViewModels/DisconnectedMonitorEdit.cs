using Waller.Native.Core.Models;
using Waller.Native.Core.Sessions;

namespace Waller.Native.App.ViewModels;

internal sealed record DisconnectedMonitorEditResult(
    ActiveSession? Session,
    string StatusText);

internal static class DisconnectedMonitorEdit
{
    public static DisconnectedMonitorEditResult Forget(
        ActiveSessionEditor editor,
        ActiveSession session,
        MissingMonitorRowViewModel monitor,
        MonitorEditTextPresenter text) =>
        new(
            editor.RemoveMissingAssignment(
                session,
                monitor.Assignment.SavedMonitor.MonitorKey),
            text.ForgotDisconnectedMonitor(monitor.DisplayName));

    public static DisconnectedMonitorEditResult Reassign(
        ActiveSessionEditor editor,
        ActiveSession session,
        MissingMonitorRowViewModel monitor,
        MonitorRowViewModel? target,
        MonitorEditTextPresenter text)
    {
        if (target is null)
        {
            return new(Session: null, text.SelectMonitorBeforeReassign);
        }

        return new(
            editor.ReassignMissingAssignment(
                session,
                monitor.Assignment.SavedMonitor.MonitorKey,
                target.MonitorKey),
            text.ReassignedDisconnectedMonitor(monitor.DisplayName, target.DisplayName));
    }
}
