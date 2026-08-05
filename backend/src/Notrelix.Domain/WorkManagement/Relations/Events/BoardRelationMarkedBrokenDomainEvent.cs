namespace Notrelix.Domain.WorkManagement.Relations.Events;

[EventName("work-management.board-relation-marked-broken")]
public sealed record BoardRelationMarkedBrokenDomainEvent(
    Guid AccountId,
    Guid WorkspaceId,
    Guid RelationId,
    Guid MarkedBy,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(AccountId, WorkspaceId, OccurredAt);
