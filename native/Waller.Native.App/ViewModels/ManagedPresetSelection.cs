using Waller.Native.Core.Models;

namespace Waller.Native.App.ViewModels;

internal static class ManagedPresetSelection
{
    public static string NameDraft(PresetMenuItem? item) =>
        item?.IsCurrentSetup == false ? item.Name : string.Empty;

    public static Guid? SelectedId(PresetMenuItem? item) =>
        item?.Id is Guid selectedId && PresetIds.IsValid(selectedId)
            ? selectedId
            : null;

    public static PresetDeleteConfirmation DeleteConfirmation(PresetMenuItem? item, Guid id) =>
        new(id, (item ?? throw new ArgumentNullException(nameof(item))).Name);
}
