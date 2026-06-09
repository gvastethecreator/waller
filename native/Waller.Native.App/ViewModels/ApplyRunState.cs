namespace Waller.Native.App.ViewModels;

internal sealed class ApplyRunState
{
    private CancellationTokenSource? cancellation;

    public CancellationToken Begin()
    {
        if (cancellation is not null)
        {
            throw new InvalidOperationException("Apply run already started.");
        }

        cancellation = new CancellationTokenSource();
        return cancellation.Token;
    }

    public void Cancel()
    {
        cancellation?.Cancel();
    }

    public void End()
    {
        cancellation?.Dispose();
        cancellation = null;
    }
}
