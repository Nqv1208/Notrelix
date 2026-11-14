
namespace Notrelix.Application.Features.Collaboration.Abstractions;

/// <summary>
/// Serializes the check-then-insert of one follow (one person, one target) inside the active request transaction.
/// Two concurrent follows of the same item take the same lock; the second waits until the first commits, then its
/// existence check sees the committed row and the follow is a no-op (UC-COL-H03). The unique index on resource
/// watchers is the backstop, so a racing duplicate never stores a second row.
/// </summary>
/// <remarks>
/// The lock is released when the request transaction commits or rolls back; there is no manual release.
/// Implementations must fail when no transaction is active rather than lock outside it.
/// </remarks>
public interface IWatcherWriteLock
{
    Task AcquireAsync(Guid userId, ResourceRef target, CancellationToken cancellationToken);
}
