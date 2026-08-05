namespace Notrelix.Domain.WorkManagement.Relations.Events;

[EventName("work-management.board-relation-restored")]
public sealed record BoardRelationRestoredDomainEvent(
    Guid AccountId,
    Guid WorkspaceId,
    Guid RelationId,
    Guid RestoredBy,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(AccountId, WorkspaceId, OccurredAt);
