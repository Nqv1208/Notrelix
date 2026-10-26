using Notrelix.Application.Features.WorkManagement.BoardItems.Services;
using Notrelix.Domain.WorkManagement.Items;

namespace Notrelix.Infrastructure.Data.WorkManagement;

/// <summary>
/// PostgreSQL implementation of <see cref="IBoardItemReminders"/>. The claim is one conditional upsert: the row
/// is inserted, or updated only when its reminder is a full cooldown old, and the statement returns a row only
/// when it changed one. A concurrent claim on the same (Item, sender) waits on the row lock until the first
/// transaction ends and then re-evaluates the condition against the committed row, so it loses.
/// </summary>
public sealed class PostgresBoardItemReminders(ApplicationDbContext context) : IBoardItemReminders
{
    public async Task<ReminderClaim> TryClaimAsync(
        Guid accountId, Guid workspaceId, Guid boardId, Guid itemId, Guid senderUserId, DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        if (context.Database.CurrentTransaction is null)
        {
            throw new InvalidOperationException("A reminder must be claimed inside the active request transaction.");
        }

        var id = Guid.CreateVersion7();
        var eligibleBefore = now - BoardItemReminder.Cooldown;
        var claimed = await context.Database.SqlQuery<Guid>($"""
            INSERT INTO work.board_item_reminders (id, account_id, workspace_id, board_id, item_id, sender_user_id, reminded_at)
            VALUES ({id}, {accountId}, {workspaceId}, {boardId}, {itemId}, {senderUserId}, {now})
            ON CONFLICT (item_id, sender_user_id) DO UPDATE SET reminded_at = EXCLUDED.reminded_at
            WHERE work.board_item_reminders.reminded_at <= {eligibleBefore}
            RETURNING id AS "Value"
            """).ToListAsync(cancellationToken);
        if (claimed.Count == 1) return new ReminderClaim(true, now);

        var standing = await context.BoardItemReminders
            .AsNoTracking()
            .Where(r => r.ItemId == itemId && r.SenderUserId == senderUserId)
            .Select(r => r.RemindedAt)
            .FirstAsync(cancellationToken);
        return new ReminderClaim(false, standing);
    }

    public async Task<IReadOnlyDictionary<Guid, DateTimeOffset>> LastRemindedAsync(
        Guid senderUserId, IReadOnlyCollection<Guid> itemIds, CancellationToken cancellationToken) =>
        await context.BoardItemReminders
            .AsNoTracking()
            .Where(r => r.SenderUserId == senderUserId && itemIds.Contains(r.ItemId))
            .ToDictionaryAsync(r => r.ItemId, r => r.RemindedAt, cancellationToken);
}
