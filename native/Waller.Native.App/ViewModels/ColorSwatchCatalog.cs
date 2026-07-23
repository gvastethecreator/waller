namespace Waller.Native.App.ViewModels;

internal static class ColorSwatchCatalog
{
    private static readonly string[] DefaultHexValues =
    [
        "#E8E8E8",
        "#232323",
        "#6B6B6B",
        "#4A5568",
        "#183A5A",
        "#9FC4DF",
        "#EFE8D8",
        "#B8A99A",
        "#7D8064",
        "#6F8192",
        "#80758B",
        "#FFFFFF",
    ];

    public static IReadOnlyList<ColorSwatchOption> Defaults() =>
        DefaultHexValues
            .Select(ColorSwatchOption.FromHex)
            .ToArray();
}
