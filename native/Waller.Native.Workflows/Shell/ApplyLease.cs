namespace Waller.Native.Workflows.Shell;

public sealed class ApplyLease : IDisposable
{
    private readonly object gate = new();
    private readonly CancellationTokenSource cancellation = new();
    private ShellWorkspace? owner;

    internal ApplyLease(ShellWorkspace owner)
    {
        ArgumentNullException.ThrowIfNull(owner);
        this.owner = owner;
        Token = cancellation.Token;
    }

    public CancellationToken Token { get; }

    public bool IsCancellationRequested => Token.IsCancellationRequested;

    public void Cancel()
    {
        lock (gate)
        {
            if (owner is not null)
            {
                cancellation.Cancel();
            }
        }
    }

    public void Dispose()
    {
        ShellWorkspace? workspace;
        lock (gate)
        {
            workspace = owner;
            owner = null;
        }

        if (workspace is null)
        {
            return;
        }

        workspace.ReleaseApply(this);
        cancellation.Dispose();
    }
}
