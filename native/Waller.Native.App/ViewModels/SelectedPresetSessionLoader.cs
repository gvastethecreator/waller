using Waller.Native.Core.Models;
using Waller.Native.Core.Presets;

namespace Waller.Native.App.ViewModels;

internal enum SelectedPresetLoadKind
{
    CurrentSetup,
    LoadedPreset,
    MissingPreset,
}

internal sealed record SelectedPresetLoadResult
{
    public SelectedPresetLoadResult(
        SelectedPresetLoadKind Kind,
        SelectedPresetSession? Selection,
        string DisplayName)
    {
        if (!Enum.IsDefined(Kind))
        {
            throw new ArgumentOutOfRangeException(nameof(Kind), Kind, "Unknown selected Preset load kind.");
        }

        if (Kind is SelectedPresetLoadKind.CurrentSetup or SelectedPresetLoadKind.LoadedPreset)
        {
            ArgumentNullException.ThrowIfNull(Selection);
        }
        else if (Selection is not null)
        {
            throw new ArgumentException("Missing Preset load results cannot include a selection.", nameof(Selection));
        }

        if (Kind is SelectedPresetLoadKind.LoadedPreset or SelectedPresetLoadKind.MissingPreset)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(DisplayName);
        }

        this.Kind = Kind;
        this.Selection = Selection;
        this.DisplayName = DisplayName;
    }

    public SelectedPresetLoadKind Kind { get; }

    public SelectedPresetSession? Selection { get; }

    public string DisplayName { get; }

    public static SelectedPresetLoadResult CurrentSetup(SelectedPresetSession selection) =>
        new(SelectedPresetLoadKind.CurrentSetup, selection, DisplayName: string.Empty);

    public static SelectedPresetLoadResult LoadedPreset(SelectedPresetSession selection, string presetName) =>
        new(SelectedPresetLoadKind.LoadedPreset, selection, presetName);

    public static SelectedPresetLoadResult MissingPreset(string presetName) =>
        new(SelectedPresetLoadKind.MissingPreset, Selection: null, presetName);

    public bool ShouldRefreshPresetList => Kind == SelectedPresetLoadKind.MissingPreset;

    public bool TryGetSelection(out SelectedPresetSession selection)
    {
        if (Selection is { } selectedSession)
        {
            selection = selectedSession;
            return true;
        }

        selection = null!;
        return false;
    }

    public string StatusText(PresetTextPresenter text)
    {
        return Kind switch
        {
            SelectedPresetLoadKind.CurrentSetup => text.CurrentSetupSelected,
            SelectedPresetLoadKind.LoadedPreset => text.Loaded(DisplayName),
            SelectedPresetLoadKind.MissingPreset => text.NotFound(DisplayName),
            _ => string.Empty,
        };
    }
}

internal static class SelectedPresetSessionLoader
{
    public static async Task<SelectedPresetLoadResult> LoadAsync(
        PresetStore presetStore,
        PresetMatcher presetMatcher,
        ActiveSession activeSession,
        PresetMenuItem item)
    {
        ArgumentNullException.ThrowIfNull(presetStore);
        ArgumentNullException.ThrowIfNull(presetMatcher);
        ArgumentNullException.ThrowIfNull(activeSession);
        ArgumentNullException.ThrowIfNull(item);

        if (item.IsCurrentSetup)
        {
            return SelectedPresetLoadResult.CurrentSetup(
                SelectedPresetSessionFactory.CurrentSetup(activeSession));
        }

        var preset = await presetStore.LoadAsync(item.Id!.Value);
        if (preset is null)
        {
            return SelectedPresetLoadResult.MissingPreset(item.Name);
        }

        return SelectedPresetLoadResult.LoadedPreset(
            SelectedPresetSessionFactory.FromPreset(activeSession, preset, presetMatcher),
            preset.Name);
    }
}
