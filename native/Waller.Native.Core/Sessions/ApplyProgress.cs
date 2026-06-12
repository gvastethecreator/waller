using Waller.Native.Core.Models;

namespace Waller.Native.Core.Sessions;

public delegate void ApplyProgressHandler(ApplyProgress progress);

public sealed record ApplyProgress
{
    private MonitorApplyStatus status;

    public ApplyProgress(
        int Completed,
        int Total,
        string MonitorName,
        MonitorApplyStatus Status)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(Completed);
        ArgumentOutOfRangeException.ThrowIfNegative(Total);
        if (Completed > Total)
        {
            throw new ArgumentOutOfRangeException(nameof(Completed), "Completed cannot exceed Total.");
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(MonitorName);

        this.Completed = Completed;
        this.Total = Total;
        this.MonitorName = MonitorName;
        this.Status = Status;
    }

    public int Completed { get; }

    public int Total { get; }

    public string MonitorName { get; }

    public MonitorApplyStatus Status
    {
        get => status;
        init
        {
            if (!Enum.IsDefined(value))
            {
                throw new ArgumentOutOfRangeException(nameof(value), value, "Monitor apply status is invalid.");
            }

            status = value;
        }
    }
}
