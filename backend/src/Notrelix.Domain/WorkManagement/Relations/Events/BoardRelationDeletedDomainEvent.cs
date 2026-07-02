namespace Notrelix.Domain.WorkManagement.Relations.Events;

public sealed record BoardRelationDeletedDomainEvent(
    Guid AccountId,
    Guid WorkspaceId,
    Guid RelationId,
    Guid DeletedBy,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(AccountId, WorkspaceId, OccurredAt, DeletedBy);
