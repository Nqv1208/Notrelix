namespace Notrelix.Application.Features.WorkManagement.BoardItems.Services;

/// <summary>
/// The "Đã xem" ledger (one row per Item and person). A narrow port so the first insert of a person is one
/// atomic upsert: two simultaneous first views cannot collide on the unique (Item, person) index.
/// </summary>
public interface IBoardItemViews
{
    /// <summary>
    /// Records that <paramref name="userId"/> opened the Item at <paramref name="now"/>: inserts the row, or
    /// rewrites it only when the last view is at least <see cref="BoardItemView.Throttle"/> old. Must run in the
    /// request transaction.
    /// </summary>
    Task RecordAsync(
        Guid accountId, Guid workspaceId, Guid boardId, Guid itemId, Guid userId, DateTimeOffset now,
        CancellationToken cancellationToken);
}
