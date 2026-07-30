namespace Notrelix.Domain.Workspaces.Spaces.Events;

[EventName("workspaces.space-deleted")]
public sealed record SpaceDeletedDomainEvent(
    Guid AccountId,
    Guid WorkspaceId,
    Guid SpaceId,
    Guid DeletedBy,
    SpaceStatus Status,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(AccountId, WorkspaceId, OccurredAt);
