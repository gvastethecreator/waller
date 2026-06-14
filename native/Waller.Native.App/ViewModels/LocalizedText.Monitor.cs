using Waller.Native.Core.Models;

namespace Waller.Native.App.ViewModels;

public sealed partial record LocalizedText
{
    public string Resolution(int width, int height) => $"{width} x {height}";

    public string Bounds(int x, int y) => $"{x}, {y}";

    public string PlacementSummary(WallpaperPlacement placement) =>
        PlacementText.Summary(placement, this);

    public string MonitorStatusSummary(
        MonitorApplyStatus applyStatus,
        string? applyError,
        string? applyErrorMessage,
        bool isMissingImageSource,
        bool hasUnsavedPresetChanges)
    {
        var saved = hasUnsavedPresetChanges ? Unsaved : Saved;
        if (isMissingImageSource)
        {
            return $"{MissingSource} - {saved}";
        }

        if (applyStatus == MonitorApplyStatus.Error && !string.IsNullOrWhiteSpace(applyError))
        {
            var label = ApplyErrorLabel(applyError);
            return string.IsNullOrWhiteSpace(applyErrorMessage)
                ? $"{Error}: {label} - {saved}"
                : $"{Error}: {label} ({applyErrorMessage}) - {saved}";
        }

        return $"{ApplyStatus(applyStatus)} - {saved}";
    }
}
