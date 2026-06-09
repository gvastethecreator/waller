namespace Waller.Native.Core.Sessions;

public sealed class ApplyCanceledException(ApplySessionResult result, Exception? innerException = null)
    : OperationCanceledException("Wallpaper apply was cancelled.", innerException)
{
    public ApplySessionResult Result { get; } = result;

    public ApplyCanceledException WithSkipped(int skipped)
    {
        return new ApplyCanceledException(Result.WithSkipped(skipped), this);
    }
}
