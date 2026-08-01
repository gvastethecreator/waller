using Waller.Native.Core.Models;

namespace Waller.Native.Workflows.MonitorEditing;

public sealed record MonitorEditorDraft
{
    public MonitorEditorDraft(
        WallpaperSourceKind SourceKind,
        string? ImagePath,
        string? ColorHex,
        WallpaperFitMode FitMode,
        WallpaperAnchor Anchor,
        double OffsetXPercent = 0,
        double OffsetYPercent = 0)
    {
        this.SourceKind = SourceKind;
        this.ImagePath = ImagePath ?? string.Empty;
        this.ColorHex = ColorHex ?? "#000000";
        this.FitMode = FitMode;
        this.Anchor = Anchor;
        this.OffsetXPercent = OffsetXPercent;
        this.OffsetYPercent = OffsetYPercent;
    }

    public WallpaperSourceKind SourceKind { get; }

    public string ImagePath { get; }

    public string ColorHex { get; }

    public WallpaperFitMode FitMode { get; }

    public WallpaperAnchor Anchor { get; }

    public double OffsetXPercent { get; }

    public double OffsetYPercent { get; }

    public static MonitorEditorDraft FromAssignment(PresetAssignment assignment)
    {
        ArgumentNullException.ThrowIfNull(assignment);

        return new MonitorEditorDraft(
            assignment.Source.Kind,
            assignment.Source.ImagePath,
            assignment.Source.ColorHex,
            assignment.Placement.FitMode,
            assignment.Placement.Anchor,
            assignment.Placement.OffsetXPercent,
            assignment.Placement.OffsetYPercent);
    }
}
