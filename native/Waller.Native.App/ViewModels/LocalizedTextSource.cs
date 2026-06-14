namespace Waller.Native.App.ViewModels;

internal static class LocalizedTextSource
{
    public static Func<LocalizedText> Require(Func<LocalizedText> text)
    {
        ArgumentNullException.ThrowIfNull(text);

        return () => text()
            ?? throw new InvalidOperationException("Localized text source returned null.");
    }
}
