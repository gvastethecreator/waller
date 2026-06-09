namespace Waller.Native.Core.Presets;

public static class PresetNames
{
    public static string DefaultName(DateTimeOffset createdAt) =>
        $"Preset {createdAt:yyyy-MM-dd HH.mm}";

    public static string Validate(string name)
    {
        var trimmed = name.Trim();
        if (string.IsNullOrWhiteSpace(trimmed))
        {
            throw new ArgumentException("Preset name is required.", nameof(name));
        }

        return trimmed;
    }

    public static string DuplicateName(string sourceName, string? requestedName)
    {
        var baseName = string.IsNullOrWhiteSpace(requestedName)
            ? Validate(sourceName)
            : Validate(requestedName);

        return $"{baseName} copy";
    }
}
