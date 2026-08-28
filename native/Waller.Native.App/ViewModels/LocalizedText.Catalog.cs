using Waller.Native.Core.Settings;

namespace Waller.Native.App.ViewModels;

public sealed partial record LocalizedText
{
    public static LocalizedText For(string language)
    {
        _ = AppLanguages.NormalizeOrDefault(language);
        return English;
    }
}
