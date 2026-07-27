namespace Notrelix.Domain.Workspaces.Spaces.Events;

[EventName("workspaces.space-unarchived")]
public sealed record SpaceUnarchivedDomainEvent(
    Guid AccountId,
    Guid WorkspaceId,
    Guid SpaceId,
    Guid UnarchivedBy,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(AccountId, WorkspaceId, OccurredAt);
