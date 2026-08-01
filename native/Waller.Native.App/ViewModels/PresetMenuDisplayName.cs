namespace Waller.Native.App.ViewModels;

internal static class PresetMenuDisplayName
{
    public static string Normalize(string name, string parameterName)
    {
        if (name is null)
        {
            throw new ArgumentNullException(parameterName);
        }

        var trimmed = name.Trim();
        if (string.IsNullOrWhiteSpace(trimmed))
        {
            throw new ArgumentException("Preset menu name is required.", parameterName);
        }

        return trimmed;
    }
}
