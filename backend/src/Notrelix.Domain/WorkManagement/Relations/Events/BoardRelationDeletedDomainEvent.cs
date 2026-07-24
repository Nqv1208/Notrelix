namespace Notrelix.Domain.WorkManagement.Relations.Events;

[EventName("work-management.board-relation-deleted")]
public sealed record BoardRelationDeletedDomainEvent(
    Guid AccountId,
    Guid WorkspaceId,
    Guid RelationId,
    Guid DeletedBy,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(AccountId, WorkspaceId, OccurredAt);
