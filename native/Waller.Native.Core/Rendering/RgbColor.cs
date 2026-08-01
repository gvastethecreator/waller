using Waller.Native.Core.Models;

namespace Waller.Native.Core.Rendering;

internal readonly record struct RgbColor(byte Red, byte Green, byte Blue)
{
    public static RgbColor Black { get; } = new(0, 0, 0);

    public static RgbColor FromHex(string colorHex)
    {
        var value = ColorHexValue.Parse(colorHex);
        return new RgbColor(value.Red, value.Green, value.Blue);
    }
}
