using Waller.Native.Core.Models;

namespace Waller.Native.Core.Sessions;

public sealed record ApplySessionResult(
    ActiveSession Session,
    int Succeeded,
    int Failed,
    int Skipped = 0)
{
    public bool HasAppliedOutcome => Succeeded > 0 || Failed > 0;

    public bool HasAnyOutcome => Succeeded > 0 || Failed > 0 || Skipped > 0;

    public ApplySessionResult WithSkipped(int skipped) =>
        this with { Skipped = skipped };
}
