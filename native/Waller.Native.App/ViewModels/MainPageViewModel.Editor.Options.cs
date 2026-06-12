namespace Waller.Native.App.ViewModels;

public sealed partial class MainPageViewModel
{
    private void RefreshEditorOptions()
    {
        var selection = LocalizedOptionSelections.RefreshEditor(
            SourceOptions,
            FitOptions,
            AnchorOptions,
            Text,
            EditSourceKind,
            EditFitMode,
            EditAnchor);
        ApplyEditorOptionSelection(selection);
    }

    private void RefreshSelectedEditorOptions()
    {
        ApplyEditorOptionSelection(LocalizedOptionSelections.SelectEditor(
            SourceOptions,
            FitOptions,
            AnchorOptions,
            EditSourceKind,
            EditFitMode,
            EditAnchor));
    }

    private void ApplyEditorOptionSelection(EditorOptionSelection selection)
    {
        SelectedSourceOption = selection.Source;
        SelectedFitOption = selection.Fit;
        SelectedAnchorOption = selection.Anchor;
    }
}
