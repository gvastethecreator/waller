using Waller.Native.Core.Models;
using Waller.Native.Core.Sessions;
using Windows.UI;

namespace Waller.Native.App.ViewModels;

internal sealed record MonitorEditDraft(
    WallpaperSourceKind SourceKind,
    string ImagePath,
    string ColorHex,
    Color Color,
    WallpaperFitMode FitMode,
    WallpaperAnchor Anchor,
    double OffsetXPercent = 0,
    double OffsetYPercent = 0)
{
    public bool IsMissingRequiredImagePath =>
        SourceKind == WallpaperSourceKind.Image && string.IsNullOrWhiteSpace(ImagePath);

    public static MonitorEditDraft FromAssignment(PresetAssignment assignment)
    {
        var colorHex = assignment.Source.ColorHex ?? "#000000";
        var color = ColorHexValue(colorHex);

        return new MonitorEditDraft(
            assignment.Source.Kind,
            assignment.Source.ImagePath ?? string.Empty,
            colorHex,
            color,
            assignment.Placement.FitMode,
            assignment.Placement.Anchor,
            assignment.Placement.OffsetXPercent,
            assignment.Placement.OffsetYPercent);
    }

    public static MonitorEditDraft FromEditorFields(
        WallpaperSourceKind sourceKind,
        string imagePath,
        string colorHex,
        Color color,
        WallpaperFitMode fitMode,
        WallpaperAnchor anchor,
        double offsetXPercent,
        double offsetYPercent) =>
        new(
            sourceKind,
            imagePath,
            colorHex,
            color,
            fitMode,
            anchor,
            offsetXPercent,
            offsetYPercent);

    public ActiveSession ApplyTo(
        ActiveSessionEditor editor,
        ActiveSession session,
        string monitorKey) =>
        editor.UpdateAssignment(
            session,
            monitorKey,
            ToSource(),
            ToPlacement());

    public WallpaperSource ToSource()
    {
        if (IsMissingRequiredImagePath)
        {
            throw new ArgumentException("Image path is required.", "imagePath");
        }

        return SourceKind switch
        {
            WallpaperSourceKind.Image => WallpaperSource.FromImage(ImagePath),
            WallpaperSourceKind.SolidColor => WallpaperSource.FromSolidColor(ColorHex),
            _ => WallpaperSource.Empty,
        };
    }

    public WallpaperPlacement ToPlacement() => new(
        FitMode,
        Anchor,
        RoundedOffset(OffsetXPercent),
        RoundedOffset(OffsetYPercent));

    private static Color ColorHexValue(string colorHex) =>
        global::Waller.Native.App.ViewModels.ColorHex.TryToColor(colorHex, out var color)
            ? color
            : Color.FromArgb(255, 0, 0, 0);

    private static int RoundedOffset(double offset) =>
        WallpaperPlacement.ClampOffset((int)Math.Round(offset, MidpointRounding.AwayFromZero));
}
