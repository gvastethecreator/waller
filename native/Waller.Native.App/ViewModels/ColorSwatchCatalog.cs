namespace Waller.Native.App.ViewModels;

internal static class ColorSwatchCatalog
{
    private static readonly string[] DefaultHexValues =
    [
        "#000000",
        "#FFFFFF",
        "#1D4ED8",
        "#16A34A",
        "#DC2626",
        "#F59E0B",
    ];

    public static IReadOnlyList<ColorSwatchOption> Defaults() =>
        DefaultHexValues
            .Select(ColorSwatchOption.FromHex)
            .ToArray();
}
