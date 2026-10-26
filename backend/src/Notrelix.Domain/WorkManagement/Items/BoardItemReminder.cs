namespace Notrelix.Domain.WorkManagement.Items;

/// <summary>
/// The last time one member asked the people in charge of an Item for an update ("Nhắc"). One row per Item and
/// sender: the 24 hour limit is a durable rule of Work Management, so it holds across requests, retries and
/// restarts instead of living in a cache. The person asked is whoever is assigned when the reminder is sent.
/// </summary>
public class BoardItemReminder : Entity, IWorkspaceScoped
{
    /// <summary>How long a sender must wait before asking again about the same Item.</summary>
    public static readonly TimeSpan Cooldown = TimeSpan.FromHours(24);

    public Guid AccountId { get; private set; }
    public Guid WorkspaceId { get; private set; }
    public Guid BoardId { get; private set; }
    public Guid ItemId { get; private set; }
    public Guid SenderUserId { get; private set; }
    public DateTimeOffset RemindedAt { get; private set; }

    private BoardItemReminder() : base() { }

    /// <summary>The first reminder this sender gives about the Item.</summary>
    public static BoardItemReminder Create(
        Guid accountId,
        Guid workspaceId,
        Guid boardId,
        Guid itemId,
        Guid senderUserId,
        DateTimeOffset remindedAt)
    {
        Guard.NotEmpty(accountId);
        Guard.NotEmpty(workspaceId);
        Guard.NotEmpty(boardId);
        Guard.NotEmpty(itemId);
        Guard.NotEmpty(senderUserId);

        return new BoardItemReminder
        {
            AccountId = accountId,
            WorkspaceId = workspaceId,
            BoardId = boardId,
            ItemId = itemId,
            SenderUserId = senderUserId,
            RemindedAt = remindedAt,
        };
    }

    /// <summary>The first moment the sender may remind about this Item again.</summary>
    public DateTimeOffset NextRemindAt => RemindedAt + Cooldown;

    public bool CanRemind(DateTimeOffset now) => now >= NextRemindAt;

    /// <summary>Records another reminder; rejected, leaving the state unchanged, while the limit still applies.</summary>
    public void Remind(DateTimeOffset now)
    {
        if (!CanRemind(now))
        {
            throw new BusinessRuleException(
                WorkManagementRuleCodes.WorkManagement_Reminder_TooSoon,
                $"The people in charge were already reminded; it is possible again at {NextRemindAt:O}.");
        }

        RemindedAt = now;
    }
}
