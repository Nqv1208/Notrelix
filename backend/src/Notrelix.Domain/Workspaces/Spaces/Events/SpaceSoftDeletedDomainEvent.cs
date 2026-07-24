namespace Notrelix.Domain.Workspaces.Spaces.Events;

[EventName("workspaces.space-soft-deleted")]
public sealed record SpaceSoftDeletedDomainEvent(
    Guid AccountId,
    Guid WorkspaceId,
    Guid SpaceId,
    Guid DeletedBy,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(AccountId, WorkspaceId, OccurredAt);
