namespace Notrelix.Domain.Collaboration.Notifications.Events;

public sealed record NotificationReadDomainEvent(
    Guid WorkspaceId,
    Guid NotificationId,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(WorkspaceId, OccurredAt, null);
