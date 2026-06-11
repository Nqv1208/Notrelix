using Notrelix.Domain.Common;
using Notrelix.Domain.SharedKernel;

namespace Notrelix.Domain.Collaboration.Notifications;

public sealed record NotificationArchivedEvent(
    Guid WorkspaceId,
    Guid NotificationId,
    DateTimeOffset OccurredAt
) : DomainEvent(OccurredAt);
