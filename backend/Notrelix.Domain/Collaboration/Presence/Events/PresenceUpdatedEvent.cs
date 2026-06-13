using Notrelix.Domain.Common;
using Notrelix.Domain.SharedKernel;

namespace Notrelix.Domain.Collaboration.Presence.Events;

public sealed record PresenceUpdatedEvent(
    Guid WorkspaceId,
    Guid UserId,
    PresenceStatus Status,
    DateTimeOffset OccurredAt
) : DomainEvent(OccurredAt, WorkspaceId, null);
