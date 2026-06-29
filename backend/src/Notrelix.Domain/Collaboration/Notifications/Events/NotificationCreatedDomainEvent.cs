namespace Notrelix.Domain.Collaboration.Notifications.Events;

public sealed record NotificationCreatedDomainEvent(
    Guid WorkspaceId,
    Guid NotificationId,
    Guid UserId,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(WorkspaceId, OccurredAt, null);
