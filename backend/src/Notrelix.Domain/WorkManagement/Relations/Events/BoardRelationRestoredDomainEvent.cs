namespace Notrelix.Domain.WorkManagement.Relations.Events;

public sealed record BoardRelationRestoredDomainEvent(
    Guid WorkspaceId,
    Guid RelationId,
    Guid RestoredBy,
    DateTimeOffset OccurredAt
) : DomainEvent(OccurredAt, WorkspaceId, RestoredBy);
