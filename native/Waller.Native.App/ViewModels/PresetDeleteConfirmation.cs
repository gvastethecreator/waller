namespace Waller.Native.App.ViewModels;

internal sealed record PresetDeleteConfirmation(Guid Id, string Name)
{
    public string Message(LocalizedText text) =>
        string.IsNullOrWhiteSpace(Name)
            ? text.DeleteSelectedPreset
            : text.Format(text.DeleteSelectedPresetFormat, Name);
}
