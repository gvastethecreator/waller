using Waller.Native.Core.Models;

namespace Waller.Native.Workflows.Presets;

public sealed record PresetSelection
{
    public PresetSelection(ActiveSession session, Preset? selectedPreset)
    {
        ArgumentNullException.ThrowIfNull(session);
        Session = session;
        SelectedPreset = selectedPreset;
    }

    public ActiveSession Session { get; }

    public Preset? SelectedPreset { get; }

    public bool IsCurrentSetup => SelectedPreset is null;
}

public sealed record PresetDeletion
{
    public PresetDeletion(ActiveSession session, bool deletedActivePreset)
    {
        ArgumentNullException.ThrowIfNull(session);
        Session = session;
        DeletedActivePreset = deletedActivePreset;
    }

    public ActiveSession Session { get; }

    public bool DeletedActivePreset { get; }
}
