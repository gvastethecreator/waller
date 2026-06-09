using Waller.Native.Core.Models;
using Waller.Native.Core.Sessions;
using Windows.UI;

namespace Waller.Native.App.ViewModels;

internal sealed record MonitorAssignmentUpdateResult(
    ActiveSession? Session,
    bool MissingRequiredImagePath,
    ArgumentException? InvalidEditValue)
{
    public static MonitorAssignmentUpdateResult Updated(ActiveSession session) =>
        new(session, MissingRequiredImagePath: false, InvalidEditValue: null);

    public static MonitorAssignmentUpdateResult MissingImagePath() =>
        new(Session: null, MissingRequiredImagePath: true, InvalidEditValue: null);

    public static MonitorAssignmentUpdateResult InvalidValue(ArgumentException error) =>
        new(Session: null, MissingRequiredImagePath: false, error);

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
        if (MissingRequiredImagePath)
        {
            return text.ImagePathRequired;
        }

        if (InvalidEditValue is { } error)
        {
            return text.InvalidEditValue(error);
        }

        return text.PendingChanges(monitorName);
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
