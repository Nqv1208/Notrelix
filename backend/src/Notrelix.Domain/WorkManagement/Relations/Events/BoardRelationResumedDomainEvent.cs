namespace Notrelix.Domain.WorkManagement.Relations.Events;

public sealed record BoardRelationResumedDomainEvent(
    Guid AccountId,
    Guid WorkspaceId,
    Guid RelationId,
    Guid ResumedBy,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(AccountId, WorkspaceId, OccurredAt, ResumedBy);
