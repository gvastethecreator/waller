using Microsoft.UI.Xaml.Media;

namespace Waller.Native.App.ViewModels;

public sealed record ColorSwatchOption(string Hex, SolidColorBrush Brush)
{
    public static ColorSwatchOption FromHex(string hex) => new(hex, ColorHex.BrushFromHex(hex));
}
