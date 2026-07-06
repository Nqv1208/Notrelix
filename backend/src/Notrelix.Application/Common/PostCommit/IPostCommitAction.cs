namespace Notrelix.Application.Common.PostCommit;

public interface IPostCommitAction
{
    Task ExecuteAsync(CancellationToken ct);
}
