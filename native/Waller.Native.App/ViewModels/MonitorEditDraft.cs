using Waller.Native.Core.Models;
using Waller.Native.Core.Sessions;
using Windows.UI;

namespace Waller.Native.App.ViewModels;

internal sealed record MonitorEditDraft
{
    public MonitorEditDraft(
        WallpaperSourceKind SourceKind,
        string? ImagePath,
        string? ColorHex,
        Color Color,
        WallpaperFitMode FitMode,
        WallpaperAnchor Anchor,
        double OffsetXPercent = 0,
        double OffsetYPercent = 0)
    {
        if (!Enum.IsDefined(SourceKind))
        {
            throw new ArgumentOutOfRangeException(nameof(SourceKind), SourceKind, "Unknown editor source kind.");
        }

        if (!Enum.IsDefined(FitMode))
        {
            throw new ArgumentOutOfRangeException(nameof(FitMode), FitMode, "Unknown editor fit mode.");
        }

        if (!Enum.IsDefined(Anchor))
        {
            throw new ArgumentOutOfRangeException(nameof(Anchor), Anchor, "Unknown editor anchor.");
        }

        if (!double.IsFinite(OffsetXPercent))
        {
            throw new ArgumentOutOfRangeException(nameof(OffsetXPercent), OffsetXPercent, "Editor X offset must be finite.");
        }

        if (!double.IsFinite(OffsetYPercent))
        {
            throw new ArgumentOutOfRangeException(nameof(OffsetYPercent), OffsetYPercent, "Editor Y offset must be finite.");
        }

        this.SourceKind = SourceKind;
        this.ImagePath = ImagePath ?? string.Empty;
        this.ColorHex = SourceKind == WallpaperSourceKind.SolidColor
            ? global::Waller.Native.Core.Models.ColorHexValue.Normalize(ColorHex ?? string.Empty)
            : ColorHex ?? "#000000";
        this.Color = Color;
        this.FitMode = FitMode;
        this.Anchor = Anchor;
        this.OffsetXPercent = OffsetXPercent;
        this.OffsetYPercent = OffsetYPercent;
    }

    public WallpaperSourceKind SourceKind { get; }

    public string ImagePath { get; }

    public string ColorHex { get; }

    public Color Color { get; }

    public WallpaperFitMode FitMode { get; }

    public WallpaperAnchor Anchor { get; }

    public double OffsetXPercent { get; }

    public double OffsetYPercent { get; }

    public bool IsMissingRequiredImagePath =>
        SourceKind == WallpaperSourceKind.Image && string.IsNullOrWhiteSpace(ImagePath);

    public static MonitorEditDraft FromAssignment(PresetAssignment assignment)
    {
        ArgumentNullException.ThrowIfNull(assignment);

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
        (editor ?? throw new ArgumentNullException(nameof(editor))).UpdateAssignment(
            session ?? throw new ArgumentNullException(nameof(session)),
            string.IsNullOrWhiteSpace(monitorKey)
                ? throw new ArgumentException("Monitor key is required.", nameof(monitorKey))
                : monitorKey,
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
