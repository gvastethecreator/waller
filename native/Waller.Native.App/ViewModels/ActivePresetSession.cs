using Waller.Native.Core.Models;

namespace Waller.Native.App.ViewModels;

internal static class ActivePresetSession
{
    public static ActivePresetRename RenameActive(ActiveSession session, Preset renamed) =>
        new(
            RenameActivePreset(session, renamed),
            renamed,
            renamed.Name);

    public static bool IsBasedOn(ActiveSession session, Guid presetId) =>
        session.BasedOnPreset?.Id == presetId;

    public static ActiveSession MarkSaved(ActiveSession session, Preset preset) =>
        session.WithSavedPreset(preset.Identity);

    public static ActiveSession SelectCurrentSetup(ActiveSession session) =>
        session with
        {
            BasedOnPreset = null,
            HasUnsavedPresetChanges = false,
            MissingAssignments = [],
        };

    public static ActiveSession ClearDeletedActivePreset(ActiveSession session) =>
        session with
        {
            BasedOnPreset = null,
            HasUnsavedPresetChanges = true,
        };

    public static ActiveSession RenameActivePreset(ActiveSession session, Preset renamed) =>
        session with { BasedOnPreset = renamed.Identity };
}

internal sealed record ActivePresetRename(
    ActiveSession Session,
    Preset SelectedPresetRecord,
    string PresetNameDraft);
