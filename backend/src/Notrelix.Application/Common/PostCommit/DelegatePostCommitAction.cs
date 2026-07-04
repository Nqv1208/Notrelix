namespace Notrelix.Application.Common.PostCommit;

public sealed class DelegatePostCommitAction : IPostCommitAction
{
    private readonly Func<CancellationToken, Task> _action;

    public DelegatePostCommitAction(Func<CancellationToken, Task> action)
    {
        _action = action ?? throw new ArgumentNullException(nameof(action));
    }

    public Task ExecuteAsync(CancellationToken ct) => _action(ct);
}
