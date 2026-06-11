using Notrelix.Domain.Common;
using Notrelix.Domain.SharedKernel;

namespace Notrelix.Domain.Collaboration.Notifications;

public sealed record NotificationCreatedEvent(
    Guid WorkspaceId,
    Guid NotificationId,
    Guid UserId,
    DateTimeOffset OccurredAt
) : DomainEvent(OccurredAt);
