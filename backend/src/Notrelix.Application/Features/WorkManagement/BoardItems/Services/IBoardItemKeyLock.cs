namespace Notrelix.Application.Features.WorkManagement.BoardItems.Services;

/// <summary>
/// Serializes Item-key allocation for one Board inside the active request
/// transaction, so concurrent creates read the Board's committed sequence one
/// after another and never draw the same number. The lock is released when that
/// transaction ends; implementations must fail when no transaction is active.
/// </summary>
public interface IBoardItemKeyLock
{
    Task AcquireAsync(Guid boardId, CancellationToken cancellationToken);
}
