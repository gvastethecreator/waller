namespace Waller.Native.App.ViewModels;

internal sealed class MainPageTextPresenters
{
    public MainPageTextPresenters(Func<LocalizedText> text)
    {
        var source = LocalizedTextSource.Require(text);

        Apply = new ApplyTextPresenter(source);
        Preset = new PresetTextPresenter(source);
        MonitorEdit = new MonitorEditTextPresenter(source);
        Shell = new ShellStatusTextPresenter(source);
    }

    public ApplyTextPresenter Apply { get; }

    public PresetTextPresenter Preset { get; }

    public MonitorEditTextPresenter MonitorEdit { get; }

    public ShellStatusTextPresenter Shell { get; }
}
