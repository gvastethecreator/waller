using Waller.Native.Core.Settings;

namespace Waller.Native.App.ViewModels;

public sealed partial record LocalizedText
{
    public static LocalizedText For(string language) =>
        string.Equals(language, AppLanguages.Spanish, StringComparison.OrdinalIgnoreCase)
            ? Spanish
            : English;
}
