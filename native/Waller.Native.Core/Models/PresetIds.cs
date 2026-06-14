namespace Waller.Native.Core.Models;

public static class PresetIds
{
    public static bool IsValid(Guid id) => id != Guid.Empty;

    public static Guid? NormalizeOptional(Guid? id) =>
        id == Guid.Empty ? null : id;

    public static Guid RequireValid(Guid id, string parameterName)
    {
        if (!IsValid(id))
        {
            throw new ArgumentException("Preset id cannot be empty.", parameterName);
        }

        return id;
    }
}
