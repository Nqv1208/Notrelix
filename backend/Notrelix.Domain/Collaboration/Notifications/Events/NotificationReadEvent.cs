using Notrelix.Domain.Common;
using Notrelix.Domain.SharedKernel;

namespace Notrelix.Domain.Collaboration.Notifications.Events;

public sealed record NotificationReadEvent(
    Guid WorkspaceId,
    Guid NotificationId,
    DateTimeOffset OccurredAt
) : DomainEvent(OccurredAt);
