namespace Notrelix.Domain.WorkManagement.Items;

/// <summary>
/// The last time one member asked the people in charge of an Item for an update ("Nhắc"). One row per Item and
/// sender: the 24 hour limit is a durable rule of Work Management, so it holds across requests, retries and
/// restarts. The rule is enforced atomically by the store that claims the row (a conditional upsert), never by
/// reading the row and then writing it, so two simultaneous presses cannot both pass.
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

    /// <summary>The first moment the sender may remind about this Item again.</summary>
    public DateTimeOffset NextRemindAt => NextAfter(RemindedAt);

    /// <summary>The first moment a sender may remind again after a reminder sent at <paramref name="remindedAt"/>.</summary>
    public static DateTimeOffset NextAfter(DateTimeOffset remindedAt) => remindedAt + Cooldown;
}
