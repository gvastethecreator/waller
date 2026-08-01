using Waller.Native.Core.Models;

namespace Waller.Native.App.ViewModels;

internal sealed record PresetDeleteConfirmation
{
    public PresetDeleteConfirmation(Guid Id, string Name)
    {
        this.Id = PresetIds.RequireValid(Id, nameof(Id));
        this.Name = PresetMenuDisplayName.Normalize(Name, nameof(Name));
    }

    public Guid Id { get; }

    public string Name { get; }

    public string Message(LocalizedText text) =>
        (text ?? throw new ArgumentNullException(nameof(text)))
            .Format(text.DeleteSelectedPresetFormat, Name);
}
