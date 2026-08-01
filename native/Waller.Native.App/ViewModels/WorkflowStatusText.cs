namespace Waller.Native.App.ViewModels;

internal static class WorkflowStatusText
{
    public static string Require(string statusText, string parameterName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(statusText, parameterName);
        return statusText;
    }
}
