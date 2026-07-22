namespace Notrelix.Domain.Workspaces.Spaces.Events;

public sealed record SpaceTypeChangedDomainEvent(
    Guid AccountId,
    Guid WorkspaceId,
    Guid SpaceId,
    SpaceType OldType,
    SpaceType NewType,
    Guid UpdatedBy,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(AccountId, WorkspaceId, OccurredAt);
