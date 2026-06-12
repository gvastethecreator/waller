using Microsoft.UI.Xaml.Media;

namespace Waller.Native.App.ViewModels;

public sealed record ColorSwatchOption
{
    public ColorSwatchOption(string Hex, SolidColorBrush Brush)
    {
        this.Hex = Waller.Native.Core.Models.ColorHexValue.Normalize(Hex);
        this.Brush = Brush ?? throw new ArgumentNullException(nameof(Brush));
    }

    public string Hex { get; }

    public SolidColorBrush Brush { get; }

    public static ColorSwatchOption FromHex(string hex) => new(hex, ColorHex.BrushFromHex(hex));
}
