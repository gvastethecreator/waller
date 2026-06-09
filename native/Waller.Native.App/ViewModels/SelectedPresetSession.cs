using Waller.Native.Core.Models;
using Waller.Native.Core.Presets;

namespace Waller.Native.App.ViewModels;

internal sealed record SelectedPresetSession(
    ActiveSession Session,
    Preset? SelectedPresetRecord,
    Guid? LastSelectedPresetId,
    string PresetNameDraft,
    Guid? PersistPresetId,
    bool PersistVisualMemory,
    bool SelectFirst);

internal static class SelectedPresetSessionFactory
{
    public static SelectedPresetSession CurrentSetup(ActiveSession session) =>
        new(
            ActivePresetSession.SelectCurrentSetup(session),
            SelectedPresetRecord: null,
            LastSelectedPresetId: null,
            PresetNameDraft: string.Empty,
            PersistPresetId: null,
            PersistVisualMemory: true,
            SelectFirst: false);

    public static SelectedPresetSession FromPreset(
        ActiveSession session,
        Preset preset,
        PresetMatcher matcher) =>
        new(
            matcher.ApplyPreset(session, preset),
            preset,
            preset.Id,
            preset.Name,
            preset.Id,
            PersistVisualMemory: true,
            SelectFirst: true);

    public static SelectedPresetSession DeletedActivePreset(ActiveSession session) =>
        new(
            ActivePresetSession.ClearDeletedActivePreset(session),
            SelectedPresetRecord: null,
            LastSelectedPresetId: null,
            PresetNameDraft: string.Empty,
            PersistPresetId: null,
            PersistVisualMemory: false,
            SelectFirst: false);
}
