using Waller.Native.Core.Models;

namespace Waller.Native.Core.Presets;

internal static class PresetFilePolicy
{
    public static Preset NormalizeForSave(Preset preset, DateTimeOffset savedAt)
    {
        ArgumentNullException.ThrowIfNull(preset);

        return preset with
        {
            SchemaVersion = Preset.CurrentSchemaVersion,
            Name = PresetNames.Validate(preset.Name),
            Assignments = PresetAssignments.Normalize(preset.Assignments),
            UpdatedAt = savedAt,
            CreatedAt = preset.CreatedAt == default ? savedAt : preset.CreatedAt,
        };
    }

    public static Preset? NormalizeLoaded(Preset? preset)
    {
        if (preset is null
            || preset.SchemaVersion != Preset.CurrentSchemaVersion
            || preset.Id == Guid.Empty
            || string.IsNullOrWhiteSpace(preset.Name)
            || preset.Assignments is null)
        {
            return null;
        }

        var assignments = PresetAssignments.TryNormalize(preset.Assignments);
        if (assignments is null)
        {
            return null;
        }

        var (createdAt, updatedAt) = NormalizeTimestamps(preset.CreatedAt, preset.UpdatedAt);

        return preset with
        {
            SchemaVersion = Preset.CurrentSchemaVersion,
            Name = PresetNames.Validate(preset.Name),
            Assignments = assignments,
            CreatedAt = createdAt,
            UpdatedAt = updatedAt,
        };
    }

    private static (DateTimeOffset CreatedAt, DateTimeOffset UpdatedAt) NormalizeTimestamps(
        DateTimeOffset createdAt,
        DateTimeOffset updatedAt)
    {
        var created = createdAt == default ? updatedAt : createdAt;
        if (created == default)
        {
            created = DateTimeOffset.UnixEpoch;
        }

        var updated = updatedAt == default || updatedAt < created
            ? created
            : updatedAt;

        return (created, updated);
    }
}
