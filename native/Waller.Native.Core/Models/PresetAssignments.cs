namespace Waller.Native.Core.Models;

public static class PresetAssignments
{
    public static PresetAssignment Normalize(PresetAssignment assignment)
    {
        var source = WallpaperSource.TryNormalize(assignment.Source)
            ?? throw new ArgumentException("Preset assignment source is invalid.", nameof(assignment));

        return assignment with
        {
            Source = source,
            Placement = assignment.Placement.NormalizeOffsets(),
        };
    }

    public static IReadOnlyList<PresetAssignment> Normalize(IReadOnlyList<PresetAssignment> assignments)
    {
        return TryNormalize(assignments)
            ?? throw new ArgumentException("Preset assignments are invalid.", nameof(assignments));
    }

    public static IReadOnlyList<PresetAssignment>? TryNormalize(IReadOnlyList<PresetAssignment>? assignments)
    {
        if (assignments is null)
        {
            return null;
        }

        var seen = MonitorKeys.CreateSet();
        var normalized = new List<PresetAssignment>();

        foreach (var assignment in assignments)
        {
            var source = WallpaperSource.TryNormalize(assignment?.Source);
            if (!IsValid(assignment, source))
            {
                return null;
            }

            if (seen.Add(assignment!.SavedMonitor.MonitorKey))
            {
                normalized.Add(assignment! with
                {
                    Source = source!,
                    Placement = assignment.Placement.NormalizeOffsets(),
                });
            }
        }

        return normalized;
    }

    private static bool IsValid(PresetAssignment? assignment, WallpaperSource? source) =>
        assignment?.SavedMonitor is { IsValidForPresetAssignment: true }
        && source is not null
        && assignment.Placement is not null
        && Enum.IsDefined(assignment.Placement.FitMode)
        && Enum.IsDefined(assignment.Placement.Anchor);
}
