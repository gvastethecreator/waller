using Waller.Native.Core.Models;
using Waller.Native.Core.Sessions;

namespace Waller.Native.App.ViewModels;

public sealed partial record LocalizedText
{
    public string ApplyResultSummary(ApplySessionResult result)
    {
        var summary = result.HasAppliedOutcome
            ? Format(ApplyFinishedFormat, result.Succeeded, result.Failed)
            : result.HasAnyOutcome
                ? NothingApplied
                : NothingToApply;

        return result.Skipped == 0
            ? summary
            : $"{summary} {Format(SkippedMissingSourceFormat, result.Skipped)}";
    }

    public string ApplyProgressSummary(ApplyProgress progress) =>
        progress.Total == 0
            ? NothingToApply
            : $"{ApplyStatus(progress.Status)} {progress.MonitorName} ({progress.Completed}/{progress.Total})";

    public string ApplyStatus(MonitorApplyStatus status) => status switch
    {
        MonitorApplyStatus.Clean => Clean,
        MonitorApplyStatus.Pending => Pending,
        MonitorApplyStatus.Applying => Applying,
        MonitorApplyStatus.Applied => Applied,
        MonitorApplyStatus.Error => Error,
        _ => CheckValue,
    };

    public string ApplyErrorLabel(string applyError) => applyError switch
    {
        ApplyErrorCodes.MissingImageSource => MissingSource,
        ApplyErrorCodes.RenderedWallpaperMissing => RenderedWallpaperMissing,
        ApplyErrorCodes.WallpaperApplyFailed => WallpaperApplyFailed,
        _ => UnknownApplyError,
    };
}
