using Waller.Native.Core.Models;
using Waller.Native.Core.Sessions;
using Windows.UI;

namespace Waller.Native.App.ViewModels;

internal sealed record MonitorAssignmentUpdateResult
{
    public MonitorAssignmentUpdateResult(
        ActiveSession? Session,
        bool MissingRequiredImagePath,
        ArgumentException? InvalidEditValue)
    {
        if (Session is not null && (MissingRequiredImagePath || InvalidEditValue is not null))
        {
            throw new ArgumentException("Successful monitor assignment updates cannot include validation failures.");
        }

        if (Session is null && MissingRequiredImagePath == (InvalidEditValue is not null))
        {
            throw new ArgumentException("Failed monitor assignment updates must include exactly one validation failure.");
        }

        this.Session = Session;
        this.MissingRequiredImagePath = MissingRequiredImagePath;
        this.InvalidEditValue = InvalidEditValue;
    }

    public ActiveSession? Session { get; }

    public bool MissingRequiredImagePath { get; }

    public ArgumentException? InvalidEditValue { get; }

    public static MonitorAssignmentUpdateResult Updated(ActiveSession session) =>
        new(session ?? throw new ArgumentNullException(nameof(session)), MissingRequiredImagePath: false, InvalidEditValue: null);

    public static MonitorAssignmentUpdateResult MissingImagePath() =>
        new(Session: null, MissingRequiredImagePath: true, InvalidEditValue: null);

    public static MonitorAssignmentUpdateResult InvalidValue(ArgumentException error) =>
        new(Session: null, MissingRequiredImagePath: false, error ?? throw new ArgumentNullException(nameof(error)));

    public bool TryGetUpdatedSession(out ActiveSession session)
    {
        if (Session is { } updatedSession)
        {
            session = updatedSession;
            return true;
        }

        session = null!;
        return false;
    }

    public string StatusText(MonitorEditTextPresenter text, string monitorName)
    {
        ArgumentNullException.ThrowIfNull(text);

        if (MissingRequiredImagePath)
        {
            return text.ImagePathRequired;
        }

        if (InvalidEditValue is { } error)
        {
            return text.InvalidEditValue(error);
        }

        return text.PendingChanges(monitorName ?? throw new ArgumentNullException(nameof(monitorName)));
    }
}

internal static class MonitorAssignmentUpdate
{
    public static MonitorAssignmentUpdateResult ApplyFromEditorFields(
        ActiveSessionEditor editor,
        ActiveSession session,
        string monitorKey,
        WallpaperSourceKind sourceKind,
        string imagePath,
        string colorHex,
        Color color,
        WallpaperFitMode fitMode,
        WallpaperAnchor anchor,
        double offsetXPercent,
        double offsetYPercent)
    {
        ArgumentNullException.ThrowIfNull(editor);
        ArgumentNullException.ThrowIfNull(session);
        ArgumentException.ThrowIfNullOrWhiteSpace(monitorKey);

        try
        {
            var draft = MonitorEditDraft.FromEditorFields(
                sourceKind,
                imagePath,
                colorHex,
                color,
                fitMode,
                anchor,
                offsetXPercent,
                offsetYPercent);
            if (draft.IsMissingRequiredImagePath)
            {
                return MonitorAssignmentUpdateResult.MissingImagePath();
            }

            return MonitorAssignmentUpdateResult.Updated(
                draft.ApplyTo(editor, session, monitorKey));
        }
        catch (ArgumentException error)
        {
            return MonitorAssignmentUpdateResult.InvalidValue(error);
        }
    }
}
