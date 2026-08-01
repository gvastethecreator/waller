using Waller.Native.Core.Models;

namespace Waller.Native.Workflows.Apply;

public enum ApplyWorkflowTarget
{
    AllReadySources,
    MonitorReadySource,
}

public sealed record ApplyWorkflowRequest
{
    private ApplyWorkflowRequest(ApplyWorkflowTarget target, string? monitorKey)
    {
        Target = DefinedEnumValue.Require(target, nameof(target), "Apply target is not supported.");
        MonitorKey = target == ApplyWorkflowTarget.MonitorReadySource
            ? MonitorKeys.Require(monitorKey ?? string.Empty, nameof(monitorKey))
            : null;
    }

    public ApplyWorkflowTarget Target { get; }

    public string? MonitorKey { get; }

    public static ApplyWorkflowRequest AllReadySources() =>
        new(ApplyWorkflowTarget.AllReadySources, monitorKey: null);

    public static ApplyWorkflowRequest MonitorReadySource(string monitorKey) =>
        new(ApplyWorkflowTarget.MonitorReadySource, monitorKey);
}
