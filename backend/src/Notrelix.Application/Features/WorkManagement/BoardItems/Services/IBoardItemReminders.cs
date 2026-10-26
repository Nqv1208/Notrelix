namespace Notrelix.Application.Features.WorkManagement.BoardItems.Services;

/// <summary>The outcome of asking to record a reminder: whether this caller won it, and the time on the row.</summary>
public readonly record struct ReminderClaim(bool Claimed, DateTimeOffset RemindedAt)
{
    public DateTimeOffset NextRemindAt => BoardItemReminder.NextAfter(RemindedAt);
}

/// <summary>
/// The durable record of who reminded whom about which Item and when (one row per Item and sender). A narrow
/// port: the reminder ledger is not part of the Work persistence port every Work use case shares.
/// </summary>
public interface IBoardItemReminders
{
    /// <summary>
    /// Records a reminder at <paramref name="now"/> only if the sender has none for the Item or the last one is a
    /// full cooldown old, in one atomic step: of any number of simultaneous callers exactly one claims it. A caller
    /// that does not claim gets the time of the reminder that stands. Must run in the request transaction, so a
    /// claim and the notifications sent for it commit or fail together.
    /// </summary>
    Task<ReminderClaim> TryClaimAsync(
        Guid accountId, Guid workspaceId, Guid boardId, Guid itemId, Guid senderUserId, DateTimeOffset now,
        CancellationToken cancellationToken);

    /// <summary>When the sender last reminded about each listed Item; Items never reminded are absent.</summary>
    Task<IReadOnlyDictionary<Guid, DateTimeOffset>> LastRemindedAsync(
        Guid senderUserId, IReadOnlyCollection<Guid> itemIds, CancellationToken cancellationToken);
}
