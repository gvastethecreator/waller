namespace Waller.Native.Workflows.Presets;

public enum PresetOperationStatus
{
    Success,
    Missing,
    WriteFailed,
}

public sealed record PresetOperationResult<T> where T : class
{
    private PresetOperationResult(T? value, PresetOperationStatus status)
    {
        if ((value is null) == (status == PresetOperationStatus.Success))
        {
            throw new ArgumentException("A Preset operation result must contain only a successful value.");
        }

        Value = value;
        Status = status;
    }

    public T? Value { get; }

    public PresetOperationStatus Status { get; }

    public bool Succeeded => Status == PresetOperationStatus.Success;

    public static PresetOperationResult<T> Success(T value) =>
        new(value ?? throw new ArgumentNullException(nameof(value)), PresetOperationStatus.Success);

    public static PresetOperationResult<T> Missing() =>
        new(null, PresetOperationStatus.Missing);

    public static PresetOperationResult<T> WriteFailed() =>
        new(null, PresetOperationStatus.WriteFailed);

    public bool TryGetValue(out T value)
    {
        value = Value!;
        return Succeeded;
    }
}
