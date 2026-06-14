namespace Waller.Native.App.ViewModels;

internal sealed class MonitorEditTextPresenter
{
    private readonly Func<LocalizedText> text;

    public MonitorEditTextPresenter(Func<LocalizedText> text)
    {
        this.text = LocalizedTextSource.Require(text);
    }

    public string ImageSelectionCancelled => text().ImageSelectionCancelled;

    public string SelectMonitorBeforeReassign => text().SelectMonitorBeforeReassign;

    public string ImagePathRequired => text().ImagePathRequired;

    public string SelectedImage(string fileName) =>
        text().Format(text().SelectedImageFormat, ImageDisplayName.Normalize(fileName, nameof(fileName)));

    public string ForgotDisconnectedMonitor(string monitorName) =>
        text().Format(text().ForgotDisconnectedMonitorFormat, NormalizeMonitorName(monitorName, nameof(monitorName)));

    public string ReassignedDisconnectedMonitor(string monitorName, string targetName) =>
        text().Format(
            text().ReassignedDisconnectedMonitorFormat,
            NormalizeMonitorName(monitorName, nameof(monitorName)),
            NormalizeMonitorName(targetName, nameof(targetName)));

    public string InvalidEditValue(ArgumentException error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return text().Format(text().InvalidEditValueFormat, text().ValidationMessage(error));
    }

    public string PendingChanges(string monitorName) =>
        text().Format(text().PendingChangesFormat, NormalizeMonitorName(monitorName, nameof(monitorName)));

    private static string NormalizeMonitorName(string monitorName, string parameterName)
    {
        if (monitorName is null)
        {
            throw new ArgumentNullException(parameterName);
        }

        var trimmed = monitorName.Trim();
        if (trimmed.Length == 0)
        {
            throw new ArgumentException("Monitor display name is required.", parameterName);
        }

        return trimmed;
    }
}
