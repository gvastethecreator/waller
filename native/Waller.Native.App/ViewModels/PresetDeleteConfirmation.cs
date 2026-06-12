namespace Waller.Native.App.ViewModels;

internal sealed record PresetDeleteConfirmation
{
    public PresetDeleteConfirmation(Guid Id, string Name)
    {
        if (Id == Guid.Empty)
        {
            throw new ArgumentException("Preset delete confirmation id is required.", nameof(Id));
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(Name);

        this.Id = Id;
        this.Name = Name;
    }

    public Guid Id { get; }

    public string Name { get; }

    public string Message(LocalizedText text) =>
        (text ?? throw new ArgumentNullException(nameof(text)))
            .Format(text.DeleteSelectedPresetFormat, Name);
}
