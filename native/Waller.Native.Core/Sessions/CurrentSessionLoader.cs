using Waller.Native.Core.Models;
using Waller.Native.Core.Windows;

namespace Waller.Native.Core.Sessions;

public sealed record CurrentSessionLoadResult
{
    public CurrentSessionLoadResult(ActiveSession Session, bool UsedFallback)
    {
        ArgumentNullException.ThrowIfNull(Session);

        this.Session = Session;
        this.UsedFallback = UsedFallback;
    }

    public ActiveSession Session { get; }

    public bool UsedFallback { get; }
}

public static class CurrentSessionLoader
{
    public static async Task<CurrentSessionLoadResult> LoadAsync(
        IMonitorDetector primaryMonitorDetector,
        IMonitorDetector fallbackMonitorDetector,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(primaryMonitorDetector);
        ArgumentNullException.ThrowIfNull(fallbackMonitorDetector);

        var primarySession = await TryLoadPrimarySessionAsync(
            primaryMonitorDetector,
            cancellationToken);
        if (primarySession is not null)
        {
            return new CurrentSessionLoadResult(primarySession, UsedFallback: false);
        }

        var fallbackSession = await new ActiveSessionFactory(fallbackMonitorDetector)
            .CreateFromCurrentWindowsStateAsync(cancellationToken);
        return new CurrentSessionLoadResult(fallbackSession, UsedFallback: true);
    }

    private static async Task<ActiveSession?> TryLoadPrimarySessionAsync(
        IMonitorDetector monitorDetector,
        CancellationToken cancellationToken)
    {
        try
        {
            var detectedSession = await new ActiveSessionFactory(monitorDetector)
                .CreateFromCurrentWindowsStateAsync(cancellationToken);
            return detectedSession.Monitors.Count == 0 ? null : detectedSession;
        }
        catch (Exception error) when (error is not OperationCanceledException)
        {
            return null;
        }
    }
}
