namespace Notrelix.Domain.WorkManagement.Relations.Events;

public sealed record BoardRelationMarkedBrokenDomainEvent(
    Guid WorkspaceId,
    Guid RelationId,
    Guid MarkedBy,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(WorkspaceId, OccurredAt, MarkedBy);
