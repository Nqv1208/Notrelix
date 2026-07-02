namespace Notrelix.Domain.Workspaces.Spaces.Events;

public sealed record SpaceMovedDomainEvent(
    Guid AccountId,
    Guid SpaceId,
    Guid OldWorkspaceId,
    Guid NewWorkspaceId,
    Guid MovedBy,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(AccountId, NewWorkspaceId, OccurredAt, MovedBy);
