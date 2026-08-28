using System.Globalization;

namespace Waller.Native.Core.Settings;

public static class AppLanguages
{
    public const string English = "en";
    // Kept only to migrate persisted settings from builds that offered Spanish.
    public const string Spanish = "es";

    public static IReadOnlyList<string> Supported { get; } = [English];

    public static string NormalizeOrDefault(string? language)
    {
        return Normalize(language) ?? English;
    }

    public static string? Normalize(string? language)
    {
        if (string.Equals(language, English, StringComparison.OrdinalIgnoreCase))
        {
            return English;
        }

        if (string.Equals(language, Spanish, StringComparison.OrdinalIgnoreCase))
        {
            return English;
        }

        return null;
    }

    public static CultureInfo CultureFor(string? language) =>
        CultureInfo.GetCultureInfo(NormalizeOrDefault(language));
}
