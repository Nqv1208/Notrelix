namespace Notrelix.Application.Features.Collaboration.Abstractions;

/// <summary>
/// Serializes the check-then-insert of one reaction (one person, one emoji, one comment) inside the active
/// request transaction. Two concurrent adds of the same reaction take the same lock; the second waits until the
/// first commits, then its existence check sees the committed row and the add is a no-op (UC-COL-A09). The
/// unique index on reactions is the backstop; this lock is what makes the duplicate a no-op instead of a
/// unique-violation.
/// </summary>
/// <remarks>
/// The lock is released when the request transaction commits or rolls back; there is no manual release.
/// Implementations must fail when no transaction is active rather than lock outside it.
/// </remarks>
public interface IReactionWriteLock
{
    Task AcquireAsync(Guid commentId, Guid userId, string emojiCode, CancellationToken cancellationToken);
}
