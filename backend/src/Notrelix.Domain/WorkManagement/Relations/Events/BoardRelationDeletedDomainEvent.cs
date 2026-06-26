namespace Notrelix.Domain.WorkManagement.Relations.Events;

public sealed record BoardRelationDeletedDomainEvent(
    Guid WorkspaceId,
    Guid RelationId,
    Guid DeletedBy,
    DateTimeOffset OccurredAt
) : DomainEvent(OccurredAt, WorkspaceId, DeletedBy);
