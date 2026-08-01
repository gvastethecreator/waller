using Microsoft.UI.Xaml.Media;
using Waller.Native.Core.Models;
using Windows.UI;

namespace Waller.Native.App.ViewModels;

public static class ColorHex
{
    public static string FromColor(Color color) => $"#{color.R:X2}{color.G:X2}{color.B:X2}";

    public static bool TryToColor(string? colorHex, out Color color)
    {
        color = Color.FromArgb(0, 0, 0, 0);
        if (ColorHexValue.TryParse(colorHex, out var value))
        {
            color = Color.FromArgb(255, value.Red, value.Green, value.Blue);
            return true;
        }

        return false;
    }

    public static SolidColorBrush BrushFromHex(string? colorHex)
    {
        return TryToColor(colorHex, out var color)
            ? new SolidColorBrush(color)
            : new SolidColorBrush(Color.FromArgb(0, 0, 0, 0));
    }
}
