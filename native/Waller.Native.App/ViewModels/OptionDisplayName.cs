namespace Waller.Native.App.ViewModels;

internal static class OptionDisplayName
{
    public static string Normalize(string displayName, string parameterName)
    {
        if (displayName is null)
        {
            throw new ArgumentNullException(parameterName);
        }

        var trimmed = displayName.Trim();
        if (string.IsNullOrWhiteSpace(trimmed))
        {
            throw new ArgumentException("Option display name is required.", parameterName);
        }

        return trimmed;
    }
}
