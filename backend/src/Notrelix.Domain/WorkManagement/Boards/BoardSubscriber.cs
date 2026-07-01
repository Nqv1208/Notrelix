namespace Notrelix.Domain.WorkManagement.Boards;

public enum BoardSubscriberRole
{
    Owner,
    Subscriber,
    Guest
}

public class BoardSubscriber : Entity, IWorkspaceScoped
{
    public Guid AccountId { get; private set; }
    public Guid WorkspaceId { get; private set; }
    public Guid BoardId { get; private set; }
    public Guid UserId { get; private set; }
    public BoardSubscriberRole SubscriberRole { get; private set; }
    public string NotificationJson { get; private set; } = "{}";
    public DateTimeOffset SubscribedAt { get; private set; }
    public Guid? SubscribedBy { get; private set; }
    public long Version { get; private set; } = 1;

    private BoardSubscriber() : base() { }

    public static BoardSubscriber Create(
        Guid accountId,
        Guid workspaceId,
        Guid boardId,
        Guid userId,
        BoardSubscriberRole role,
        DateTimeOffset subscribedAt,
        Guid? subscribedBy,
        string? notificationJson = null)
    {
        Guard.NotEmpty(workspaceId);
        Guard.NotEmpty(boardId);
        Guard.NotEmpty(userId);
        Guard.NotEmpty(accountId);

        return new BoardSubscriber
        {
            AccountId = accountId,
            WorkspaceId = workspaceId,
            BoardId = boardId,
            UserId = userId,
            SubscriberRole = role,
            SubscribedAt = subscribedAt,
            SubscribedBy = subscribedBy,
            NotificationJson = notificationJson ?? "{}"
        };
    }

    public void UpdateNotificationSettings(string notificationJson)
    {
        NotificationJson = notificationJson ?? "{}";
        Version++;
    }
}
