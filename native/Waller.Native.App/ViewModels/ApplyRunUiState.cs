using Waller.Native.Core.Models;
using Waller.Native.Core.Sessions;

namespace Waller.Native.App.ViewModels;

internal sealed record ApplyRunUiState(
    ActiveSession? Session,
    string ProgressText,
    string StatusText,
    bool Succeeded)
{
    public static ApplyRunUiState Success(ApplySessionResult result, ApplyTextPresenter text) =>
        new(result.Session, string.Empty, text.Result(result), Succeeded: true);

    public static ApplyRunUiState Cancelled(ApplySessionResult? result, ApplyTextPresenter text) =>
        new(result?.Session, string.Empty, text.Cancelled, Succeeded: false);

    public static ApplyRunUiState UnexpectedFailure(ApplyTextPresenter text) =>
        new(null, string.Empty, text.UnexpectedFailure, Succeeded: false);

    public static ApplyRunUiState FromException(Exception error, ApplyTextPresenter text) =>
        error switch
        {
            ApplyCanceledException cancelled => Cancelled(cancelled.Result, text),
            OperationCanceledException => Cancelled(result: null, text),
            _ => UnexpectedFailure(text),
        };
}
