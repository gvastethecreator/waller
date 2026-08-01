namespace Waller.Native.Core.Models;

public static class MonitorIdentityMatcher
{
    public const int FallbackPositionTolerance = 32;

    public static bool IsFallbackCandidate(MonitorIdentity saved, MonitorIdentity current) =>
        saved.Width == current.Width
        && saved.Height == current.Height
        && Math.Abs(saved.X - current.X) <= FallbackPositionTolerance
        && Math.Abs(saved.Y - current.Y) <= FallbackPositionTolerance;

    public static int FallbackDistance(MonitorIdentity saved, MonitorIdentity current) =>
        Math.Abs(saved.X - current.X) + Math.Abs(saved.Y - current.Y);
}
