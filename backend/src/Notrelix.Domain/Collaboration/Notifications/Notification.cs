using Notrelix.Domain.Common;
using Notrelix.Domain.Common.Exceptions;

namespace Notrelix.Domain.Collaboration.Notifications;

public class Notification : AggregateRoot, IWorkspaceScoped
{
    public Guid UserId { get; private set; }
    public Guid WorkspaceId { get; private set; }
    public NotificationType Type { get; private set; }
    public string Title { get; private set; } = null!;
    public string Content { get; private set; } = null!;
    public ResourceRef? Target { get; private set; }
    public bool IsRead { get; private set; }
    public DateTimeOffset? ReadAt { get; private set; }
    public bool IsArchived { get; private set; }
    public DateTimeOffset? ArchivedAt { get; private set; }

    private Notification() : base() { }

    public static Notification Create(
        Guid userId,
        Guid workspaceId,
        NotificationType type,
        string title,
        string content,
        DateTimeOffset createdAt,
        ResourceRef? target = null)
    {
        Guard.NotEmpty(userId);
        Guard.NotEmpty(workspaceId);
        Guard.NotNullOrWhiteSpace(title);
        Guard.NotNullOrWhiteSpace(content);

        if (target?.WorkspaceId.HasValue == true && target.WorkspaceId.Value != workspaceId)
            throw new WorkspaceMismatchException(workspaceId, target.WorkspaceId.Value);

        var notification = new Notification
        {
            UserId = userId,
            WorkspaceId = workspaceId,
            Type = type,
            Title = title.Trim(),
            Content = content.Trim(),
            Target = target,
            IsRead = false,
            IsArchived = false
        };

        notification.SetAuditOnCreate(userId, createdAt);
        return notification;
    }

    public void MarkAsRead(DateTimeOffset readAt)
    {
        if (IsRead) return;
        if (IsArchived)
            throw new BusinessRuleException("Cannot read an archived notification.");

        IsRead = true;
        ReadAt = readAt;
        SetAuditOnUpdate(UserId, readAt);
        IncrementVersion();
        AddDomainEvent(new NotificationReadDomainEvent(WorkspaceId, Id, readAt));
    }

    public void Archive(DateTimeOffset archivedAt)
    {
        if (IsArchived) return;

        IsArchived = true;
        ArchivedAt = archivedAt;
        SetAuditOnUpdate(UserId, archivedAt);
        IncrementVersion();
        AddDomainEvent(new NotificationArchivedDomainEvent(WorkspaceId, Id, archivedAt));
    }
}
