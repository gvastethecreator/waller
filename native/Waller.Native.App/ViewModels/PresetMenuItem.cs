namespace Waller.Native.App.ViewModels;

public sealed record PresetMenuItem
{
    private Guid? id;
    private string name = string.Empty;

    public PresetMenuItem(Guid? Id, string Name)
    {
        if (string.IsNullOrWhiteSpace(Name))
        {
            throw new ArgumentException("Preset menu name is required.", nameof(Name));
        }

        this.Id = Id;
        name = Name;
    }

    public Guid? Id
    {
        get => id;
        init
        {
            if (value == Guid.Empty)
            {
                throw new ArgumentException("Preset menu id cannot be empty.", nameof(value));
            }

            id = value;
        }
    }

    public string Name
    {
        get => name;
        init
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException("Preset menu name is required.", nameof(value));
            }

            name = value;
        }
    }

    public bool IsCurrentSetup => Id is null;

    public static PresetMenuItem CreateCurrentSetup(string name) => new(null, name);

    public override string ToString() => Name;
}
