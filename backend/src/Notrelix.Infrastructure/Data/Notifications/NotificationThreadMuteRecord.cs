namespace Notrelix.Infrastructure.Data.Notifications;

/// <summary>
/// One person's choice to hear nothing more from the thread of one resource (an Item or a page) in one
/// Workspace. A muted thread produces no new notification for that person; earlier ones stay as they are.
/// </summary>
public sealed class NotificationThreadMuteRecord
{
    public Guid Id { get; private set; }
    public Guid AccountId { get; private set; }
    public Guid WorkspaceId { get; private set; }
    public Guid UserId { get; private set; }
    public string ResourceKind { get; private set; } = null!;
    public Guid ResourceId { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    private NotificationThreadMuteRecord() { }

    public static NotificationThreadMuteRecord Create(
        Guid accountId, Guid workspaceId, Guid userId, string resourceKind, Guid resourceId, DateTimeOffset createdAt) =>
        new()
        {
            Id = Guid.CreateVersion7(),
            AccountId = accountId,
            WorkspaceId = workspaceId,
            UserId = userId,
            ResourceKind = resourceKind.Trim(),
            ResourceId = resourceId,
            CreatedAt = createdAt
        };
}
