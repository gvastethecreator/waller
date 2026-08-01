namespace Waller.Native.Core.Models;

public sealed record ApplyResult
{
    private ApplyResult(
        MonitorIdentity monitor,
        bool Succeeded,
        string? ErrorCode,
        string? ErrorMessage)
    {
        ArgumentNullException.ThrowIfNull(monitor);

        this.Monitor = monitor;
        this.Succeeded = Succeeded;
        this.ErrorCode = Succeeded
            ? null
            : ApplyErrorCodes.Normalize(ErrorCode);
        this.ErrorMessage = Succeeded ? null : ErrorMessage;
    }

    public MonitorIdentity Monitor { get; }

    public bool Succeeded { get; }

    public string? ErrorCode { get; }

    public string? ErrorMessage { get; }

    public static ApplyResult Success(MonitorIdentity monitor)
    {
        ArgumentNullException.ThrowIfNull(monitor);

        return new(monitor, Succeeded: true, ErrorCode: null, ErrorMessage: null);
    }

    public static ApplyResult Failure(MonitorIdentity monitor, string? errorCode, string? errorMessage = null)
    {
        ArgumentNullException.ThrowIfNull(monitor);

        return new(
            monitor,
            Succeeded: false,
            ErrorCode: errorCode,
            ErrorMessage: errorMessage);
    }
}
