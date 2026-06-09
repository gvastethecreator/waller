namespace Waller.Native.App.ViewModels;

internal sealed class MainPageTextPresenters(Func<LocalizedText> text)
{
    public ApplyTextPresenter Apply { get; } = new(text);

    public PresetTextPresenter Preset { get; } = new(text);

    public MonitorEditTextPresenter MonitorEdit { get; } = new(text);

    public ShellStatusTextPresenter Shell { get; } = new(text);
}
