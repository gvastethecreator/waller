using Waller.Native.Core.Models;

namespace Waller.Native.App.ViewModels;

internal static class EditorOffsetPercent
{
    private const string OffsetXParameterName = "OffsetXPercent";
    private const string OffsetYParameterName = "OffsetYPercent";
    private const string OffsetXFiniteErrorMessage = "Editor X offset must be finite.";
    private const string OffsetYFiniteErrorMessage = "Editor Y offset must be finite.";

    public static double NormalizeX(double offsetPercent) =>
        Normalize(offsetPercent, OffsetXParameterName, OffsetXFiniteErrorMessage);

    public static double NormalizeY(double offsetPercent) =>
        Normalize(offsetPercent, OffsetYParameterName, OffsetYFiniteErrorMessage);

    public static double Normalize(
        double offsetPercent,
        string parameterName,
        string finiteErrorMessage)
    {
        if (!double.IsFinite(offsetPercent))
        {
            throw new ArgumentOutOfRangeException(parameterName, offsetPercent, finiteErrorMessage);
        }

        return Math.Clamp(offsetPercent, -100d, 100d);
    }

    public static int ToPlacementOffsetX(double offsetPercent) =>
        ToPlacementOffset(offsetPercent, OffsetXParameterName, OffsetXFiniteErrorMessage);

    public static int ToPlacementOffsetY(double offsetPercent) =>
        ToPlacementOffset(offsetPercent, OffsetYParameterName, OffsetYFiniteErrorMessage);

    public static int ToPlacementOffset(
        double offsetPercent,
        string parameterName,
        string finiteErrorMessage)
    {
        var normalized = Normalize(offsetPercent, parameterName, finiteErrorMessage);
        return WallpaperPlacement.ClampOffset(
            (int)Math.Round(normalized, MidpointRounding.AwayFromZero));
    }
}
