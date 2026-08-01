using Waller.Native.Core.Sessions;

namespace Waller.Native.Workflows.Apply;

public enum ApplyWorkflowStatus
{
    Completed,
    Cancelled,
    AlreadyRunning,
    Unavailable,
    UnexpectedFailure,
}

public sealed record ApplyWorkflowResult
{
    private ApplyWorkflowResult(
        ApplyWorkflowStatus status,
        ApplySessionResult? result = null,
        Exception? error = null)
    {
        if (status == ApplyWorkflowStatus.Completed && result is null)
        {
            throw new ArgumentException("A completed Apply must contain its technical result.");
        }

        if (result is not null
            && status is not ApplyWorkflowStatus.Completed and not ApplyWorkflowStatus.Cancelled)
        {
            throw new ArgumentException("Only completed or cancelled Apply outcomes can contain a session result.");
        }

        if ((status == ApplyWorkflowStatus.UnexpectedFailure) != (error is not null))
        {
            throw new ArgumentException("Only an unexpected Apply failure can contain an exception.");
        }

        Status = status;
        Result = result;
        Error = error;
    }

    public ApplyWorkflowStatus Status { get; }

    public ApplySessionResult? Result { get; }

    public Exception? Error { get; }

    public bool Succeeded => Status == ApplyWorkflowStatus.Completed;

    public static ApplyWorkflowResult Completed(ApplySessionResult result) =>
        new(ApplyWorkflowStatus.Completed, result ?? throw new ArgumentNullException(nameof(result)));

    public static ApplyWorkflowResult Cancelled(ApplySessionResult? result = null) =>
        new(ApplyWorkflowStatus.Cancelled, result);

    public static ApplyWorkflowResult AlreadyRunning() =>
        new(ApplyWorkflowStatus.AlreadyRunning);

    public static ApplyWorkflowResult Unavailable() =>
        new(ApplyWorkflowStatus.Unavailable);

    public static ApplyWorkflowResult UnexpectedFailure(Exception error) =>
        new(ApplyWorkflowStatus.UnexpectedFailure, error: error ?? throw new ArgumentNullException(nameof(error)));
}
