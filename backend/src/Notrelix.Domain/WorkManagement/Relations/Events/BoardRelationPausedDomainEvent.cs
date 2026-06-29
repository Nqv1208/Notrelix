namespace Notrelix.Domain.WorkManagement.Relations.Events;

public sealed record BoardRelationPausedDomainEvent(
    Guid WorkspaceId,
    Guid RelationId,
    Guid PausedBy,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(WorkspaceId, OccurredAt, PausedBy);
