using Waller.Native.Core.Models;
using Waller.Native.Core.Sessions;

namespace Waller.Native.Workflows.MonitorEditing;

public sealed class MonitorEditorWorkflow
{
    private readonly ActiveSessionEditor editor = new();

    public MonitorDraftResult Select(ActiveSession session, string monitorKey)
    {
        ArgumentNullException.ThrowIfNull(session);
        monitorKey = MonitorKeys.Require(monitorKey, nameof(monitorKey));

        var monitor = session.Monitors.FirstOrDefault(candidate =>
            MonitorKeys.Equals(candidate.Monitor.Identity.MonitorKey, monitorKey));

        return monitor is null
            ? MonitorDraftResult.MonitorMissing()
            : MonitorDraftResult.Ready(MonitorEditorDraft.FromAssignment(monitor.DesiredAssignment));
    }

    public MonitorEditorResult Update(
        ActiveSession session,
        string monitorKey,
        MonitorEditorDraft draft)
    {
        ArgumentNullException.ThrowIfNull(session);
        monitorKey = MonitorKeys.Require(monitorKey, nameof(monitorKey));
        ArgumentNullException.ThrowIfNull(draft);

        if (!ContainsMonitor(session, monitorKey))
        {
            return MonitorEditorResult.MonitorMissing();
        }

        try
        {
            var sourceResult = CreateSource(draft);
            if (sourceResult.MissingImagePath is not null)
            {
                return MonitorEditorResult.ImageMissing(sourceResult.MissingImagePath);
            }

            var placement = new WallpaperPlacement(
                draft.FitMode,
                draft.Anchor,
                ToPlacementOffset(draft.OffsetXPercent, nameof(draft.OffsetXPercent)),
                ToPlacementOffset(draft.OffsetYPercent, nameof(draft.OffsetYPercent)))
                .NormalizeOffsets();
            var updated = editor.UpdateAssignment(
                session,
                monitorKey,
                sourceResult.Source!,
                placement);

            return ReferenceEquals(updated, session)
                ? MonitorEditorResult.Unchanged()
                : MonitorEditorResult.Updated(updated);
        }
        catch (ArgumentException error)
        {
            return MonitorEditorResult.InvalidValue(error);
        }
    }

    public MonitorEditorResult ForgetDisconnected(ActiveSession session, string monitorKey)
    {
        ArgumentNullException.ThrowIfNull(session);
        monitorKey = MonitorKeys.Require(monitorKey, nameof(monitorKey));

        if (!ContainsMissingAssignment(session, monitorKey))
        {
            return MonitorEditorResult.DisconnectedAssignmentMissing();
        }

        return MonitorEditorResult.Updated(editor.RemoveMissingAssignment(session, monitorKey));
    }

    public MonitorEditorResult ReassignDisconnected(
        ActiveSession session,
        string missingMonitorKey,
        string targetMonitorKey)
    {
        ArgumentNullException.ThrowIfNull(session);
        missingMonitorKey = MonitorKeys.Require(missingMonitorKey, nameof(missingMonitorKey));
        targetMonitorKey = MonitorKeys.Require(targetMonitorKey, nameof(targetMonitorKey));

        if (!ContainsMissingAssignment(session, missingMonitorKey))
        {
            return MonitorEditorResult.DisconnectedAssignmentMissing();
        }

        if (!ContainsMonitor(session, targetMonitorKey))
        {
            return MonitorEditorResult.TargetMonitorMissing();
        }

        return MonitorEditorResult.Updated(
            editor.ReassignMissingAssignment(session, missingMonitorKey, targetMonitorKey));
    }

    private static SourceResult CreateSource(MonitorEditorDraft draft)
    {
        DefinedEnumValue.Require(
            draft.SourceKind,
            nameof(draft.SourceKind),
            "Unknown monitor editor source kind.");

        if (draft.SourceKind != WallpaperSourceKind.Image)
        {
            return new SourceResult(draft.SourceKind switch
            {
                WallpaperSourceKind.Empty => WallpaperSource.Empty,
                WallpaperSourceKind.SolidColor => WallpaperSource.FromSolidColor(draft.ColorHex),
                _ => throw new ArgumentOutOfRangeException(
                    nameof(draft.SourceKind),
                    draft.SourceKind,
                    "Unknown monitor editor source kind."),
            });
        }

        if (string.IsNullOrWhiteSpace(draft.ImagePath))
        {
            return new SourceResult(string.Empty);
        }

        var source = WallpaperSource.FromImage(draft.ImagePath);
        return WallpaperSourceFiles.HasExistingImageFile(source)
            ? new SourceResult(source)
            : new SourceResult(source.ImagePath!);
    }

    private static int ToPlacementOffset(double value, string parameterName)
    {
        if (!double.IsFinite(value))
        {
            throw new ArgumentOutOfRangeException(parameterName, value, "Monitor editor offset must be finite.");
        }

        var normalized = Math.Clamp(value, -100d, 100d);
        return WallpaperPlacement.ClampOffset(
            (int)Math.Round(normalized, MidpointRounding.AwayFromZero));
    }

    private static bool ContainsMonitor(ActiveSession session, string monitorKey) =>
        session.Monitors.Any(candidate =>
            MonitorKeys.Equals(candidate.Monitor.Identity.MonitorKey, monitorKey));

    private static bool ContainsMissingAssignment(ActiveSession session, string monitorKey) =>
        session.MissingAssignments.Any(candidate =>
            MonitorKeys.Equals(candidate.SavedMonitor.MonitorKey, monitorKey));

    private sealed record SourceResult
    {
        public SourceResult(WallpaperSource source)
        {
            Source = source ?? throw new ArgumentNullException(nameof(source));
        }

        public SourceResult(string missingImagePath)
        {
            MissingImagePath = missingImagePath ?? throw new ArgumentNullException(nameof(missingImagePath));
        }

        public WallpaperSource? Source { get; }

        public string? MissingImagePath { get; }
    }
}
