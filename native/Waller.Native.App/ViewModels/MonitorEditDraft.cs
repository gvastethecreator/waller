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
        this.SourceKind = DefinedEnumValue.Require(
            SourceKind,
            nameof(SourceKind),
            "Unknown editor source kind.");
        this.ImagePath = ImagePath ?? string.Empty;
        this.ColorHex = SourceKind == WallpaperSourceKind.SolidColor
            ? global::Waller.Native.Core.Models.ColorHexValue.Normalize(ColorHex ?? string.Empty)
            : ColorHex ?? "#000000";
        this.Color = Color;
        this.FitMode = DefinedEnumValue.Require(
            FitMode,
            nameof(FitMode),
            "Unknown editor fit mode.");
        this.Anchor = DefinedEnumValue.Require(
            Anchor,
            nameof(Anchor),
            "Unknown editor anchor.");
        this.OffsetXPercent = EditorOffsetPercent.NormalizeX(OffsetXPercent);
        this.OffsetYPercent = EditorOffsetPercent.NormalizeY(OffsetYPercent);
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
            MonitorKeys.Require(monitorKey, nameof(monitorKey)),
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
            WallpaperSourceKind.Empty => WallpaperSource.Empty,
            WallpaperSourceKind.Image => WallpaperSource.FromImage(ImagePath),
            WallpaperSourceKind.SolidColor => WallpaperSource.FromSolidColor(ColorHex),
            _ => InvalidSourceKind(SourceKind),
        };
    }

    public WallpaperPlacement ToPlacement() => new(
        FitMode,
        Anchor,
        EditorOffsetPercent.ToPlacementOffsetX(OffsetXPercent),
        EditorOffsetPercent.ToPlacementOffsetY(OffsetYPercent));

    private static Color ColorHexValue(string colorHex) =>
        global::Waller.Native.App.ViewModels.ColorHex.TryToColor(colorHex, out var color)
            ? color
            : Color.FromArgb(255, 0, 0, 0);

    private static WallpaperSource InvalidSourceKind(WallpaperSourceKind sourceKind) =>
        throw new ArgumentOutOfRangeException(nameof(sourceKind), sourceKind, "Unknown editor source kind.");

}
