using Waller.Native.Core.Sessions;

namespace Waller.Native.App.ViewModels;

internal sealed class ApplyTextPresenter
{
    private readonly Func<LocalizedText> text;

    public ApplyTextPresenter(Func<LocalizedText> text)
    {
        this.text = LocalizedTextSource.Require(text);
    }

    public string Preparing => text().PreparingApply;

    public string Cancelled => text().ApplyCancelled;

    public string UnexpectedFailure => text().ApplyUnexpectedFailure;

    public string Progress(ApplyProgress progress)
    {
        ArgumentNullException.ThrowIfNull(progress);

        return text().ApplyProgressSummary(progress);
    }

    public string Result(ApplySessionResult result)
    {
        ArgumentNullException.ThrowIfNull(result);

        return text().ApplyResultSummary(result);
    }
}
