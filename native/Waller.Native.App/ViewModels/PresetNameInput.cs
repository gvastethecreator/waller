using Waller.Native.Core.Presets;

namespace Waller.Native.App.ViewModels;

internal static class PresetNameInput
{
    public static string DraftForSaveAs(string currentDraft, DateTimeOffset createdAt) =>
        string.IsNullOrWhiteSpace(currentDraft)
            ? PresetNames.DefaultName(createdAt)
            : PresetNames.Validate(currentDraft);

    public static bool TryValidateRequired(string draft, out string name)
    {
        try
        {
            name = PresetNames.Validate(draft);
            return true;
        }
        catch (ArgumentException)
        {
            name = string.Empty;
            return false;
        }
    }

    public static bool TryValidateRequired(
        string draft,
        PresetTextPresenter text,
        out string name,
        out string statusText)
    {
        ArgumentNullException.ThrowIfNull(text);

        if (TryValidateRequired(draft, out name))
        {
            statusText = string.Empty;
            return true;
        }

        statusText = text.NameRequired;
        return false;
    }
}
