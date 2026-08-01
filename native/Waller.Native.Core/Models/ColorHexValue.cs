using System.Text.RegularExpressions;

namespace Waller.Native.Core.Models;

public readonly record struct ColorHexValue(byte Red, byte Green, byte Blue)
{
    private static readonly Regex HexColorPattern = new("^#[0-9a-fA-F]{6}$", RegexOptions.Compiled);

    public static ColorHexValue Parse(string colorHex)
    {
        var normalized = Normalize(colorHex);
        return new ColorHexValue(
            Convert.ToByte(normalized[1..3], 16),
            Convert.ToByte(normalized[3..5], 16),
            Convert.ToByte(normalized[5..7], 16));
    }

    public static bool TryParse(string? colorHex, out ColorHexValue value)
    {
        value = default;
        if (string.IsNullOrWhiteSpace(colorHex))
        {
            return false;
        }

        try
        {
            value = Parse(colorHex);
            return true;
        }
        catch (ArgumentException)
        {
            return false;
        }
        catch (FormatException)
        {
            return false;
        }
        catch (OverflowException)
        {
            return false;
        }
    }

    public static string Normalize(string colorHex)
    {
        if (string.IsNullOrWhiteSpace(colorHex))
        {
            throw new ArgumentException("Color must be #RRGGBB.", nameof(colorHex));
        }

        var value = colorHex.Trim();
        if (!value.StartsWith('#'))
        {
            value = $"#{value}";
        }

        if (!HexColorPattern.IsMatch(value))
        {
            throw new ArgumentException("Color must be #RRGGBB.", nameof(colorHex));
        }

        return value.ToLowerInvariant();
    }

    public string ToHex() => $"#{Red:x2}{Green:x2}{Blue:x2}";
}
