namespace Waller.Native.App.ViewModels;

internal sealed class PresetTextPresenter(Func<LocalizedText> text)
{
    public string SaveAsOpened => text().SaveAsOpened;

    public string ManageOpened => text().ManagePresetsOpened;

    public string SelectToRename => text().SelectPresetToRename;

    public string SelectToDuplicate => text().SelectPresetToDuplicate;

    public string SelectToDelete => text().SelectPresetToDelete;

    public string NameRequired => text().PresetNameRequired;

    public string MissingPreset => text().PresetNotFound;

    public string LoadFailed => text().PresetLoadFailed;

    public string CurrentSetupSelected => text().CurrentSetupSelected;

    public string DeletedKeptSession => text().DeletedPresetKeptSession;

    public string Saved(string name) =>
        text().Format(text().SavedPresetFormat, name);

    public string SavedNew(string name) =>
        text().Format(text().SavedNewPresetFormat, name);

    public string Renamed(string name) =>
        text().Format(text().RenamedPresetFormat, name);

    public string Duplicated(string name) =>
        text().Format(text().DuplicatedPresetFormat, name);

    public string NotFound(string name) =>
        text().Format(text().PresetNotFoundFormat, name);

    public string Loaded(string name) =>
        text().Format(text().LoadedPresetFormat, name);
}
