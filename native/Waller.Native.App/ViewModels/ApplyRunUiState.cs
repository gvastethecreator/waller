using Waller.Native.Core.Models;
using Waller.Native.Core.Sessions;

namespace Waller.Native.App.ViewModels;

internal sealed record ApplyRunUiState
{
    public ApplyRunUiState(
        ActiveSession? Session,
        string ProgressText,
        string StatusText,
        bool Succeeded)
    {
        if (Succeeded)
        {
            ArgumentNullException.ThrowIfNull(Session);
        }

        ArgumentNullException.ThrowIfNull(ProgressText);
        ArgumentException.ThrowIfNullOrWhiteSpace(StatusText);

        this.Session = Session;
        this.ProgressText = ProgressText;
        this.StatusText = StatusText;
        this.Succeeded = Succeeded;
    }

    public ActiveSession? Session { get; }

    public string ProgressText { get; }

    public string StatusText { get; }

    public bool Succeeded { get; }

    public static ApplyRunUiState Success(ApplySessionResult result, ApplyTextPresenter text) =>
        new(
            (result ?? throw new ArgumentNullException(nameof(result))).Session,
            string.Empty,
            (text ?? throw new ArgumentNullException(nameof(text))).Result(result),
            Succeeded: true);

    public static ApplyRunUiState Cancelled(ApplySessionResult? result, ApplyTextPresenter text) =>
        new(
            result?.Session,
            string.Empty,
            (text ?? throw new ArgumentNullException(nameof(text))).Cancelled,
            Succeeded: false);

    public static ApplyRunUiState UnexpectedFailure(ApplyTextPresenter text) =>
        new(
            null,
            string.Empty,
            (text ?? throw new ArgumentNullException(nameof(text))).UnexpectedFailure,
            Succeeded: false);

    public static ApplyRunUiState FromException(Exception error, ApplyTextPresenter text) =>
        (error ?? throw new ArgumentNullException(nameof(error))) switch
        {
            ApplyCanceledException cancelled => Cancelled(cancelled.Result, text),
            OperationCanceledException => Cancelled(result: null, text),
            _ => UnexpectedFailure(text),
        };
}
