namespace Notrelix.Application.Features.Operators.Public.Audit;

/// <summary>
/// Commits one operator action: the changed aggregates, their domain events (outbox) and the audit rows
/// are saved together in one transaction. A concurrent change surfaces as a conflict, never as a lost update.
/// </summary>
public interface IOperatorChangeCommit
{
    Task CommitAsync(CancellationToken cancellationToken);
}
