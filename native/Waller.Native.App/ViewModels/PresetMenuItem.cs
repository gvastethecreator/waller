using Waller.Native.Core.Models;

namespace Waller.Native.App.ViewModels;

public sealed record PresetMenuItem
{
    private Guid? id;
    private string name = string.Empty;

    public PresetMenuItem(Guid? Id, string Name)
    {
        this.Id = Id;
        this.Name = PresetMenuDisplayName.Normalize(Name, nameof(Name));
    }

    public Guid? Id
    {
        get => id;
        init
        {
            id = value is Guid presetId
                ? PresetIds.RequireValid(presetId, nameof(value))
                : null;
        }
    }

    public string Name
    {
        get => name;
        init
        {
            name = PresetMenuDisplayName.Normalize(value, nameof(value));
        }
    }

    public bool IsCurrentSetup => Id is null;

    public static PresetMenuItem CreateCurrentSetup(string name) => new(null, name);

    public override string ToString() => Name;
}
