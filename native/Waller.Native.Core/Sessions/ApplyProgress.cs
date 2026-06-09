using Waller.Native.Core.Models;

namespace Waller.Native.Core.Sessions;

public delegate void ApplyProgressHandler(ApplyProgress progress);

public sealed record ApplyProgress(
    int Completed,
    int Total,
    string MonitorName,
    MonitorApplyStatus Status);
