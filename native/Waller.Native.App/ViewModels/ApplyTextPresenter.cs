using Waller.Native.Core.Sessions;

namespace Waller.Native.App.ViewModels;

internal sealed class ApplyTextPresenter(Func<LocalizedText> text)
{
    public string Preparing => text().PreparingApply;

    public string Cancelled => text().ApplyCancelled;

    public string UnexpectedFailure => text().ApplyUnexpectedFailure;

    public string Progress(ApplyProgress progress) =>
        text().ApplyProgressSummary(progress);

    public string Result(ApplySessionResult result) =>
        text().ApplyResultSummary(result);
}
