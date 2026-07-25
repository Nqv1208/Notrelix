namespace Notrelix.Domain.Workspaces.Spaces.Events;

public sealed record SpaceVisibilityChangedDomainEvent(
    Guid AccountId,
    Guid WorkspaceId,
    Guid SpaceId,
    SpaceVisibility OldVisibility,
    SpaceVisibility NewVisibility,
    Guid UpdatedBy,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(AccountId, WorkspaceId, OccurredAt, UpdatedBy);
