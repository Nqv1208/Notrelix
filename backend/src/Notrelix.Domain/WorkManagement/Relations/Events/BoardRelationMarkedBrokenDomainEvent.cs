namespace Notrelix.Domain.WorkManagement.Relations.Events;

public sealed record BoardRelationMarkedBrokenDomainEvent(
    Guid WorkspaceId,
    Guid RelationId,
    Guid MarkedBy,
    DateTimeOffset OccurredAt
) : DomainEvent(OccurredAt, WorkspaceId, MarkedBy);
