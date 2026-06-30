using Notrelix.Domain.Common;

namespace Notrelix.Domain.Notifications.NotificationItems.Events;

public sealed record NotificationItemCreatedDomainEvent(
    Guid WorkspaceId,
    Guid NotificationItemId,
    string NotificationType,
    string Title,
    Guid? ActorUserId,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(WorkspaceId, OccurredAt, ActorUserId);
