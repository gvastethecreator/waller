using Waller.Native.Core.Models;

namespace Waller.Native.Core.Sessions;

public sealed record ApplySessionResult
{
    public ApplySessionResult(
        ActiveSession session,
        int Succeeded,
        int Failed,
        int Skipped = 0)
    {
        ArgumentNullException.ThrowIfNull(session);
        ArgumentOutOfRangeException.ThrowIfNegative(Succeeded);
        ArgumentOutOfRangeException.ThrowIfNegative(Failed);
        ArgumentOutOfRangeException.ThrowIfNegative(Skipped);

        Session = session;
        this.Succeeded = Succeeded;
        this.Failed = Failed;
        this.Skipped = Skipped;
    }

    public ActiveSession Session { get; }

    public int Succeeded { get; }

    public int Failed { get; }

    public int Skipped { get; }

    public static ApplySessionResult None(ActiveSession session) =>
        new(session, Succeeded: 0, Failed: 0);

    public static ApplySessionResult SkippedOnly(ActiveSession session, int skipped) =>
        None(session).WithSkipped(skipped);

    public bool HasAppliedOutcome => Succeeded > 0 || Failed > 0;

    public bool HasAnyOutcome => Succeeded > 0 || Failed > 0 || Skipped > 0;

    public ApplySessionResult WithSkipped(int skipped) =>
        new(Session, Succeeded, Failed, skipped);
}
