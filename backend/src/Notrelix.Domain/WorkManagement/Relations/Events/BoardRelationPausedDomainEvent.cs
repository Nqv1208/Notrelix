namespace Notrelix.Domain.WorkManagement.Relations.Events;

public sealed record BoardRelationPausedDomainEvent(
    Guid AccountId,
    Guid WorkspaceId,
    Guid RelationId,
    Guid PausedBy,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(AccountId, WorkspaceId, OccurredAt, PausedBy);
