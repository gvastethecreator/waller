using Waller.Native.Core.Models;
using Waller.Native.Core.Sessions;
using Waller.Native.Core.Windows;

namespace Waller.Native.App.ViewModels;

internal sealed record CurrentSessionLoadResult(ActiveSession Session, bool UsedFallback);

internal static class CurrentSessionLoader
{
    public static async Task<CurrentSessionLoadResult> LoadAsync(
        IMonitorDetector primaryMonitorDetector,
        IMonitorDetector fallbackMonitorDetector)
    {
        var primarySession = await TryLoadPrimarySessionAsync(primaryMonitorDetector);
        if (primarySession is not null)
        {
            return new CurrentSessionLoadResult(primarySession, UsedFallback: false);
        }

        var fallbackSession = await new ActiveSessionFactory(fallbackMonitorDetector)
            .CreateFromCurrentWindowsStateAsync();
        return new CurrentSessionLoadResult(fallbackSession, UsedFallback: true);
    }

    private static async Task<ActiveSession?> TryLoadPrimarySessionAsync(IMonitorDetector monitorDetector)
    {
        try
        {
            var detectedSession = await new ActiveSessionFactory(monitorDetector)
                .CreateFromCurrentWindowsStateAsync();
            return detectedSession.Monitors.Count == 0 ? null : detectedSession;
        }
        catch (Exception)
        {
            return null;
        }
    }
}
