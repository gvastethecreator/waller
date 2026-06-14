using System.Collections.ObjectModel;
using Waller.Native.Core.Models;

namespace Waller.Native.App.ViewModels;

internal static class PresetMenuLists
{
    public static void ReplaceMain(
        ObservableCollection<PresetMenuItem> items,
        IReadOnlyList<Preset> presets,
        string currentSetupName)
    {
        ArgumentNullException.ThrowIfNull(items);
        ArgumentNullException.ThrowIfNull(presets);
        var normalizedCurrentSetupName = PresetMenuDisplayName.Normalize(
            currentSetupName,
            nameof(currentSetupName));

        items.Clear();
        items.Add(PresetMenuItem.CreateCurrentSetup(normalizedCurrentSetupName));
        foreach (var preset in presets)
        {
            ArgumentNullException.ThrowIfNull(preset);
            items.Add(new PresetMenuItem(preset.Id, preset.Name));
        }
    }

    public static void ReplaceManage(
        ObservableCollection<PresetMenuItem> items,
        IReadOnlyList<Preset> presets)
    {
        ArgumentNullException.ThrowIfNull(items);
        ArgumentNullException.ThrowIfNull(presets);

        items.Clear();
        foreach (var preset in presets)
        {
            ArgumentNullException.ThrowIfNull(preset);
            items.Add(new PresetMenuItem(preset.Id, preset.Name));
        }
    }

    public static PresetMenuItem? Select(
        IReadOnlyList<PresetMenuItem> items,
        Guid? id)
    {
        ArgumentNullException.ThrowIfNull(items);
        var selectedId = id is Guid presetId
            ? PresetIds.RequireValid(presetId, nameof(id))
            : (Guid?)null;

        if (selectedId is null)
        {
            return FirstOrDefault(items);
        }

        return items.FirstOrDefault(item => Item(item, nameof(items)).Id == selectedId)
            ?? FirstOrDefault(items);
    }

    public static PresetMenuItem? ReplaceCurrentSetupName(
        ObservableCollection<PresetMenuItem> items,
        PresetMenuItem? selected,
        string currentSetupName)
    {
        ArgumentNullException.ThrowIfNull(items);
        var normalizedCurrentSetupName = PresetMenuDisplayName.Normalize(
            currentSetupName,
            nameof(currentSetupName));

        for (var index = 0; index < items.Count; index++)
        {
            if (!Item(items[index], nameof(items)).IsCurrentSetup)
            {
                continue;
            }

            items[index] = PresetMenuItem.CreateCurrentSetup(normalizedCurrentSetupName);
            return selected?.IsCurrentSetup == true
                ? items[index]
                : selected;
        }

        return selected;
    }

    private static PresetMenuItem? FirstOrDefault(IReadOnlyList<PresetMenuItem> items)
    {
        foreach (var item in items)
        {
            return Item(item, nameof(items));
        }

        return null;
    }

    private static PresetMenuItem Item(PresetMenuItem? item, string parameterName) =>
        item ?? throw new ArgumentException(
            "Preset menu collection cannot include null items.",
            parameterName);
}
