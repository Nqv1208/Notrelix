namespace Notrelix.Domain.WorkManagement.Relations.Events;

public sealed record BoardRelationResumedDomainEvent(
    Guid WorkspaceId,
    Guid RelationId,
    Guid ResumedBy,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(WorkspaceId, OccurredAt, ResumedBy);
