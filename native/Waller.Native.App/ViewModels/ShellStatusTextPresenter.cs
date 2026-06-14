using Waller.Native.Core.Rendering;

namespace Waller.Native.App.ViewModels;

internal sealed class ShellStatusTextPresenter
{
    private readonly Func<LocalizedText> text;

    public ShellStatusTextPresenter(Func<LocalizedText> text)
    {
        this.text = LocalizedTextSource.Require(text);
    }

    public string LoadedCurrentSetup => text().LoadedCurrentSetup;

    public string CurrentSetupRefreshed => text().CurrentSetupRefreshed;

    public string SettingsOpened => text().SettingsOpened;

    public string SettingsSaved => text().SettingsSaved;

    public string LocalDataWriteFailed => text().LocalDataWriteFailed;

    public string StartupFailed => text().StartupFailed;

    public string RenderedCacheClearSummary(RenderedCacheClearResult result) =>
        text().RenderedCacheClearSummary(result);

    public string CurrentSessionLoadResult(bool usedFallback, string successStatus, int monitorCount) =>
        usedFallback
            ? text().WindowsDetectionFallback
            : $"{WorkflowStatusText.Require(successStatus, nameof(successStatus))} {text().Format(text().MonitorCountFormat, monitorCount)}";
}
