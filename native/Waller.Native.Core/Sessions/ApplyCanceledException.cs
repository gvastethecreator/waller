namespace Waller.Native.Core.Sessions;

public sealed class ApplyCanceledException : OperationCanceledException
{
    public ApplyCanceledException(ApplySessionResult result, Exception? innerException = null)
        : base("Wallpaper apply was cancelled.", innerException)
    {
        ArgumentNullException.ThrowIfNull(result);

        Result = result;
    }

    public ApplySessionResult Result { get; }

    public ApplyCanceledException WithSkipped(int skipped)
    {
        return new ApplyCanceledException(Result.WithSkipped(skipped), this);
    }
}
