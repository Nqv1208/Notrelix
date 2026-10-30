using Notrelix.Application.Features.WorkManagement.BoardItems.Services;
using Notrelix.Domain.WorkManagement.Items;

namespace Notrelix.Infrastructure.Data.WorkManagement;

/// <summary>
/// PostgreSQL implementation of <see cref="IBoardItemViews"/>: one conditional upsert on the unique
/// (item_id, user_id) index. A concurrent first view waits on the index entry and then takes the update branch,
/// whose throttle condition it fails, so it never raises a unique violation.
/// </summary>
public sealed class PostgresBoardItemViews(ApplicationDbContext context) : IBoardItemViews
{
    public async Task RecordAsync(
        Guid accountId, Guid workspaceId, Guid boardId, Guid itemId, Guid userId, DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        if (context.Database.CurrentTransaction is null)
        {
            throw new InvalidOperationException("A view must be recorded inside the active request transaction.");
        }

        var id = Guid.CreateVersion7();
        var eligibleBefore = now - BoardItemView.Throttle;
        await context.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO work.board_item_views (id, account_id, workspace_id, board_id, item_id, user_id, last_viewed_at)
            VALUES ({id}, {accountId}, {workspaceId}, {boardId}, {itemId}, {userId}, {now})
            ON CONFLICT (item_id, user_id) DO UPDATE SET last_viewed_at = EXCLUDED.last_viewed_at
            WHERE work.board_item_views.last_viewed_at <= {eligibleBefore}
            """, cancellationToken);
    }
}
