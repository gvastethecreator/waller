using Waller.Native.Core.Models;
using Waller.Native.Core.Windows;

namespace Waller.Native.Core.Sessions;

public sealed class ActiveSessionFactory(IMonitorDetector monitorDetector)
{
    public async Task<ActiveSession> CreateFromCurrentWindowsStateAsync(
        CancellationToken cancellationToken = default)
    {
        var monitors = await monitorDetector.DetectAsync(cancellationToken);
        return ActiveSession.FromMonitors(monitors);
    }
}
