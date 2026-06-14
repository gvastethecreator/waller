using Waller.Native.Core.Models;
using Waller.Native.Core.Presets;

namespace Waller.Native.App.ViewModels;

internal sealed record SelectedPresetSession
{
    public SelectedPresetSession(
        ActiveSession Session,
        Preset? SelectedPresetRecord,
        Guid? LastSelectedPresetId,
        string PresetNameDraft,
        Guid? PersistPresetId,
        bool PersistVisualMemory,
        bool SelectFirst)
    {
        ArgumentNullException.ThrowIfNull(Session);
        ArgumentNullException.ThrowIfNull(PresetNameDraft);

        this.Session = Session;
        this.SelectedPresetRecord = SelectedPresetRecord;
        this.LastSelectedPresetId = PresetIds.NormalizeOptional(LastSelectedPresetId);
        this.PresetNameDraft = PresetNameDraft;
        this.PersistPresetId = PresetIds.NormalizeOptional(PersistPresetId);
        this.PersistVisualMemory = PersistVisualMemory;
        this.SelectFirst = SelectFirst;
    }

    public ActiveSession Session { get; }

    public Preset? SelectedPresetRecord { get; }

    public Guid? LastSelectedPresetId { get; }

    public string PresetNameDraft { get; }

    public Guid? PersistPresetId { get; }

    public bool PersistVisualMemory { get; }

    public bool SelectFirst { get; }
}

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
        PresetMatcher matcher)
    {
        ArgumentNullException.ThrowIfNull(session);
        ArgumentNullException.ThrowIfNull(preset);
        ArgumentNullException.ThrowIfNull(matcher);

        return new(
            matcher.ApplyPreset(session, preset),
            preset,
            preset.Id,
            preset.Name,
            preset.Id,
            PersistVisualMemory: true,
            SelectFirst: true);
    }

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
