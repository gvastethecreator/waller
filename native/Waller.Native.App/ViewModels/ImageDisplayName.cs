namespace Waller.Native.App.ViewModels;

internal static class ImageDisplayName
{
    public static string Normalize(string displayName, string parameterName)
    {
        if (displayName is null)
        {
            throw new ArgumentNullException(parameterName);
        }

        var trimmed = displayName.Trim();
        if (trimmed.Length == 0)
        {
            throw new ArgumentException("Image display name is required.", parameterName);
        }

        return trimmed;
    }
}
