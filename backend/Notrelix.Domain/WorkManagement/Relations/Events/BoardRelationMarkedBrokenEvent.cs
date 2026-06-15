using Notrelix.Domain.Common;

namespace Notrelix.Domain.WorkManagement.Relations.Events;

public sealed record BoardRelationMarkedBrokenEvent(
    Guid WorkspaceId,
    Guid RelationId,
    Guid MarkedBy,
    DateTimeOffset OccurredAt
) : DomainEvent(OccurredAt, WorkspaceId, MarkedBy);
