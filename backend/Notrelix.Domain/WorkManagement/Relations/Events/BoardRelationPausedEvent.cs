using Notrelix.Domain.Common;

namespace Notrelix.Domain.WorkManagement.Relations.Events;

public sealed record BoardRelationPausedEvent(
    Guid WorkspaceId,
    Guid RelationId,
    Guid PausedBy,
    DateTimeOffset OccurredAt
) : DomainEvent(OccurredAt, WorkspaceId, PausedBy);
