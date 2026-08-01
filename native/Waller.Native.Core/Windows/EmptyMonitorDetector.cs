using Waller.Native.Core.Models;

namespace Waller.Native.Core.Windows;

public sealed class EmptyMonitorDetector : IMonitorDetector
{
    public Task<IReadOnlyList<MonitorSnapshot>> DetectAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IReadOnlyList<MonitorSnapshot>>([]);
    }
}
