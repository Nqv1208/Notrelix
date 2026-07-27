namespace Notrelix.Domain.WorkManagement.Relations.Events;

[EventName("work-management.board-relation-paused")]
public sealed record BoardRelationPausedDomainEvent(
    Guid AccountId,
    Guid WorkspaceId,
    Guid RelationId,
    Guid PausedBy,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(AccountId, WorkspaceId, OccurredAt);
