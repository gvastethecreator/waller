using Waller.Native.Core.Presets;

namespace Waller.Native.App.ViewModels;

internal sealed class PresetTextPresenter
{
    private readonly Func<LocalizedText> text;

    public PresetTextPresenter(Func<LocalizedText> text)
    {
        this.text = LocalizedTextSource.Require(text);
    }

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
        text().Format(text().SavedPresetFormat, PresetNames.Validate(name, nameof(name)));

    public string SavedNew(string name) =>
        text().Format(text().SavedNewPresetFormat, PresetNames.Validate(name, nameof(name)));

    public string Renamed(string name) =>
        text().Format(text().RenamedPresetFormat, PresetNames.Validate(name, nameof(name)));

    public string Duplicated(string name) =>
        text().Format(text().DuplicatedPresetFormat, PresetNames.Validate(name, nameof(name)));

    public string NotFound(string name) =>
        text().Format(text().PresetNotFoundFormat, PresetNames.Validate(name, nameof(name)));

    public string Loaded(string name) =>
        text().Format(text().LoadedPresetFormat, PresetNames.Validate(name, nameof(name)));
}
