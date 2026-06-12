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
        ArgumentException.ThrowIfNullOrWhiteSpace(currentSetupName);

        items.Clear();
        items.Add(PresetMenuItem.CreateCurrentSetup(currentSetupName));
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
        if (id == Guid.Empty)
        {
            throw new ArgumentException("Preset menu selection id cannot be empty.", nameof(id));
        }

        if (id is null)
        {
            return items.FirstOrDefault();
        }

        return items.FirstOrDefault(item => item.Id == id) ?? items.FirstOrDefault();
    }

    public static PresetMenuItem? ReplaceCurrentSetupName(
        ObservableCollection<PresetMenuItem> items,
        PresetMenuItem? selected,
        string currentSetupName)
    {
        ArgumentNullException.ThrowIfNull(items);
        ArgumentException.ThrowIfNullOrWhiteSpace(currentSetupName);

        for (var index = 0; index < items.Count; index++)
        {
            if (!items[index].IsCurrentSetup)
            {
                continue;
            }

            items[index] = PresetMenuItem.CreateCurrentSetup(currentSetupName);
            return selected?.IsCurrentSetup == true
                ? items[index]
                : selected;
        }

        return selected;
    }
}
