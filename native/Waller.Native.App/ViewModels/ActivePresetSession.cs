using Waller.Native.Core.Models;

namespace Waller.Native.App.ViewModels;

internal static class ActivePresetSession
{
    public static ActivePresetRename RenameActive(ActiveSession session, Preset renamed)
    {
        ArgumentNullException.ThrowIfNull(session);
        ArgumentNullException.ThrowIfNull(renamed);

        return new(
            RenameActivePreset(session, renamed),
            renamed,
            renamed.Name);
    }

    public static bool IsBasedOn(ActiveSession session, Guid presetId) =>
        session.BasedOnPreset?.Id == presetId;

    public static ActiveSession MarkSaved(ActiveSession session, Preset preset)
    {
        ArgumentNullException.ThrowIfNull(session);
        ArgumentNullException.ThrowIfNull(preset);

        return session.WithSavedPreset(preset.Identity);
    }

    public static ActiveSession SelectCurrentSetup(ActiveSession session)
    {
        ArgumentNullException.ThrowIfNull(session);

        return session with
        {
            BasedOnPreset = null,
            HasUnsavedPresetChanges = false,
            MissingAssignments = [],
        };
    }

    public static ActiveSession ClearDeletedActivePreset(ActiveSession session)
    {
        ArgumentNullException.ThrowIfNull(session);

        return session with
        {
            BasedOnPreset = null,
            HasUnsavedPresetChanges = true,
        };
    }

    public static ActiveSession RenameActivePreset(ActiveSession session, Preset renamed)
    {
        ArgumentNullException.ThrowIfNull(session);
        ArgumentNullException.ThrowIfNull(renamed);

        return session with { BasedOnPreset = renamed.Identity };
    }
}

internal sealed record ActivePresetRename
{
    public ActivePresetRename(
        ActiveSession Session,
        Preset SelectedPresetRecord,
        string PresetNameDraft)
    {
        ArgumentNullException.ThrowIfNull(Session);
        ArgumentNullException.ThrowIfNull(SelectedPresetRecord);
        ArgumentException.ThrowIfNullOrWhiteSpace(PresetNameDraft);

        this.Session = Session;
        this.SelectedPresetRecord = SelectedPresetRecord;
        this.PresetNameDraft = PresetNameDraft;
    }

    public ActiveSession Session { get; }

    public Preset SelectedPresetRecord { get; }

    public string PresetNameDraft { get; }
}
