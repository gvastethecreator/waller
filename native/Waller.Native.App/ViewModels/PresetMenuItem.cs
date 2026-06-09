namespace Waller.Native.App.ViewModels;

public sealed record PresetMenuItem(Guid? Id, string Name)
{
    public bool IsCurrentSetup => Id is null;

    public static PresetMenuItem CreateCurrentSetup(string name) => new(null, name);

    public override string ToString() => Name;
}
