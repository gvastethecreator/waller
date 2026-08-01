using Waller.Native.Core.Models;
using Waller.Native.Core.Windows;

namespace Waller.Native.Core.Sessions;

public sealed class ActiveSessionFactory
{
    private readonly IMonitorDetector monitorDetector;

    public ActiveSessionFactory(IMonitorDetector monitorDetector)
    {
        ArgumentNullException.ThrowIfNull(monitorDetector);

        this.monitorDetector = monitorDetector;
    }

    public async Task<ActiveSession> CreateFromCurrentWindowsStateAsync(
        CancellationToken cancellationToken = default)
    {
        var monitors = await monitorDetector.DetectAsync(cancellationToken);
        return ActiveSession.FromMonitors(monitors);
    }
}
