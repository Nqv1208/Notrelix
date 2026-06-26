namespace Notrelix.Domain.Collaboration.Notifications.Events;

public sealed record NotificationReadDomainEvent(
    Guid WorkspaceId,
    Guid NotificationId,
    DateTimeOffset OccurredAt
) : DomainEvent(OccurredAt, WorkspaceId, null);
