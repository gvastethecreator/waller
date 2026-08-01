using Waller.Native.Core.Models;

namespace Waller.Native.Core.Windows;

public interface IMonitorDetector
{
    Task<IReadOnlyList<MonitorSnapshot>> DetectAsync(CancellationToken cancellationToken = default);
}
