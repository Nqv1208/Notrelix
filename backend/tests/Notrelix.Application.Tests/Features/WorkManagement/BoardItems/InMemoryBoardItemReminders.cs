using Notrelix.Application.Features.WorkManagement.BoardItems.Services;

namespace Notrelix.Application.Tests.Features.WorkManagement.BoardItems;

/// <summary>
/// In-memory reminder ledger whose claim is atomic like the real conditional upsert (one lock around
/// check-and-write); the real one's persistence and row locking are proven on PostgreSQL.
/// </summary>
internal sealed class InMemoryBoardItemReminders : IBoardItemReminders
{
    private readonly object _gate = new();
    private readonly Dictionary<(Guid Item, Guid Sender), DateTimeOffset> _rows = [];

    public int Count => _rows.Count;

    public DateTimeOffset? RemindedAt(Guid itemId, Guid senderUserId) =>
        _rows.TryGetValue((itemId, senderUserId), out var at) ? at : null;

    public void Seed(Guid itemId, Guid senderUserId, DateTimeOffset at) => _rows[(itemId, senderUserId)] = at;

    public async Task<ReminderClaim> TryClaimAsync(
        Guid accountId, Guid workspaceId, Guid boardId, Guid itemId, Guid senderUserId, DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        await Task.Yield(); // lets simultaneous callers interleave before the atomic step
        lock (_gate)
        {
            if (_rows.TryGetValue((itemId, senderUserId), out var standing) && standing > now - BoardItemReminder.Cooldown)
                return new ReminderClaim(false, standing);
            _rows[(itemId, senderUserId)] = now;
            return new ReminderClaim(true, now);
        }
    }

    public Task<IReadOnlyDictionary<Guid, DateTimeOffset>> LastRemindedAsync(
        Guid senderUserId, IReadOnlyCollection<Guid> itemIds, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyDictionary<Guid, DateTimeOffset>>(_rows
            .Where(r => r.Key.Sender == senderUserId && itemIds.Contains(r.Key.Item))
            .ToDictionary(r => r.Key.Item, r => r.Value));
}
