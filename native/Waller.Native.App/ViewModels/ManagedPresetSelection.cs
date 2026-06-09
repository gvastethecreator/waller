namespace Waller.Native.App.ViewModels;

internal static class ManagedPresetSelection
{
    public static string NameDraft(PresetMenuItem? item) =>
        item?.IsCurrentSetup == false ? item.Name : string.Empty;

    public static bool TryGetId(PresetMenuItem? item, out Guid id)
    {
        if (item?.Id is Guid selectedId)
        {
            id = selectedId;
            return true;
        }

        id = Guid.Empty;
        return false;
    }

    public static PresetDeleteConfirmation DeleteConfirmation(PresetMenuItem? item, Guid id) =>
        new(id, item?.Name ?? string.Empty);
}
