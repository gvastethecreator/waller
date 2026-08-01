using Waller.Native.Core.Models;

namespace Waller.Native.Workflows.MonitorEditing;

public enum MonitorEditorStatus
{
    Updated,
    Unchanged,
    MonitorMissing,
    ImageMissing,
    InvalidValue,
    DisconnectedAssignmentMissing,
    TargetMonitorMissing,
}

public sealed record MonitorEditorResult
{
    private MonitorEditorResult(
        MonitorEditorStatus status,
        ActiveSession? session = null,
        string? missingImagePath = null,
        ArgumentException? validationError = null)
    {
        if ((status == MonitorEditorStatus.Updated) != (session is not null))
        {
            throw new ArgumentException("Only an updated monitor edit can contain a replacement session.");
        }

        if ((status == MonitorEditorStatus.ImageMissing) != (missingImagePath is not null))
        {
            throw new ArgumentException("Only a missing-image result can contain an image path.");
        }

        if ((status == MonitorEditorStatus.InvalidValue) != (validationError is not null))
        {
            throw new ArgumentException("Only an invalid monitor edit can contain a validation error.");
        }

        Status = status;
        Session = session;
        MissingImagePath = missingImagePath;
        ValidationError = validationError;
    }

    public MonitorEditorStatus Status { get; }

    public ActiveSession? Session { get; }

    public string? MissingImagePath { get; }

    public ArgumentException? ValidationError { get; }

    public static MonitorEditorResult Updated(ActiveSession session) =>
        new(MonitorEditorStatus.Updated, session ?? throw new ArgumentNullException(nameof(session)));

    public static MonitorEditorResult Unchanged() => new(MonitorEditorStatus.Unchanged);

    public static MonitorEditorResult MonitorMissing() => new(MonitorEditorStatus.MonitorMissing);

    public static MonitorEditorResult ImageMissing(string imagePath) =>
        new(MonitorEditorStatus.ImageMissing, missingImagePath: imagePath ?? throw new ArgumentNullException(nameof(imagePath)));

    public static MonitorEditorResult InvalidValue(ArgumentException error) =>
        new(MonitorEditorStatus.InvalidValue, validationError: error ?? throw new ArgumentNullException(nameof(error)));

    public static MonitorEditorResult DisconnectedAssignmentMissing() =>
        new(MonitorEditorStatus.DisconnectedAssignmentMissing);

    public static MonitorEditorResult TargetMonitorMissing() =>
        new(MonitorEditorStatus.TargetMonitorMissing);

    public bool TryGetUpdatedSession(out ActiveSession session)
    {
        session = Session!;
        return Status == MonitorEditorStatus.Updated;
    }
}

public enum MonitorDraftStatus
{
    Ready,
    MonitorMissing,
}

public sealed record MonitorDraftResult
{
    private MonitorDraftResult(MonitorDraftStatus status, MonitorEditorDraft? draft)
    {
        if ((status == MonitorDraftStatus.Ready) != (draft is not null))
        {
            throw new ArgumentException("A ready monitor selection must contain one draft.");
        }

        Status = status;
        Draft = draft;
    }

    public MonitorDraftStatus Status { get; }

    public MonitorEditorDraft? Draft { get; }

    public static MonitorDraftResult Ready(MonitorEditorDraft draft) =>
        new(MonitorDraftStatus.Ready, draft ?? throw new ArgumentNullException(nameof(draft)));

    public static MonitorDraftResult MonitorMissing() =>
        new(MonitorDraftStatus.MonitorMissing, draft: null);

    public bool TryGetDraft(out MonitorEditorDraft draft)
    {
        draft = Draft!;
        return Status == MonitorDraftStatus.Ready;
    }
}
