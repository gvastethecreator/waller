namespace Waller.Native.App.ViewModels;

internal sealed class MonitorEditTextPresenter(Func<LocalizedText> text)
{
    public string ImageSelectionCancelled => text().ImageSelectionCancelled;

    public string SelectMonitorBeforeReassign => text().SelectMonitorBeforeReassign;

    public string ImagePathRequired => text().ImagePathRequired;

    public string SelectedImage(string fileName) =>
        text().Format(text().SelectedImageFormat, fileName);

    public string ForgotDisconnectedMonitor(string monitorName) =>
        text().Format(text().ForgotDisconnectedMonitorFormat, monitorName);

    public string ReassignedDisconnectedMonitor(string monitorName, string targetName) =>
        text().Format(text().ReassignedDisconnectedMonitorFormat, monitorName, targetName);

    public string InvalidEditValue(ArgumentException error) =>
        text().Format(text().InvalidEditValueFormat, text().ValidationMessage(error));

    public string PendingChanges(string monitorName) =>
        text().Format(text().PendingChangesFormat, monitorName);
}
