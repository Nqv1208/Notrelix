namespace Notrelix.Domain.Collaboration.Notifications.Events;

public sealed record NotificationArchivedDomainEvent(
    Guid WorkspaceId,
    Guid NotificationId,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(WorkspaceId, OccurredAt, null);
