namespace Notrelix.Application.Common.PostCommit;

public interface IPostCommitActionQueue
{
    void BeginScope();
    void Enqueue(IPostCommitAction action);

    IReadOnlyList<IPostCommitAction> Actions { get; }

    Task FlushAsync(CancellationToken ct);
    void Clear();
    void EndScope();
}
