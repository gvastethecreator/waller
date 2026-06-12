using Waller.Native.Core.Models;
using Waller.Native.Core.Sessions;

namespace Waller.Native.App.ViewModels;

internal sealed record DisconnectedMonitorEditResult
{
    public DisconnectedMonitorEditResult(
        ActiveSession? Session,
        string StatusText)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(StatusText);

        this.Session = Session;
        this.StatusText = StatusText;
    }

    public ActiveSession? Session { get; }

    public string StatusText { get; }
}

internal static class DisconnectedMonitorEdit
{
    public static DisconnectedMonitorEditResult Forget(
        ActiveSessionEditor editor,
        ActiveSession session,
        MissingMonitorRowViewModel monitor,
        MonitorEditTextPresenter text) =>
        new(
            (editor ?? throw new ArgumentNullException(nameof(editor))).RemoveMissingAssignment(
                session ?? throw new ArgumentNullException(nameof(session)),
                (monitor ?? throw new ArgumentNullException(nameof(monitor))).Assignment.SavedMonitor.MonitorKey),
            (text ?? throw new ArgumentNullException(nameof(text))).ForgotDisconnectedMonitor(monitor.DisplayName));

    public static DisconnectedMonitorEditResult Reassign(
        ActiveSessionEditor editor,
        ActiveSession session,
        MissingMonitorRowViewModel monitor,
        MonitorRowViewModel? target,
        MonitorEditTextPresenter text)
    {
        ArgumentNullException.ThrowIfNull(editor);
        ArgumentNullException.ThrowIfNull(session);
        ArgumentNullException.ThrowIfNull(monitor);
        ArgumentNullException.ThrowIfNull(text);

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
