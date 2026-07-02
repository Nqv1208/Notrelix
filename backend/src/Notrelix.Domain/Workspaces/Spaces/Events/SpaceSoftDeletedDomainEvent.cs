namespace Notrelix.Domain.Workspaces.Spaces.Events;

public sealed record SpaceSoftDeletedDomainEvent(
    Guid AccountId,
    Guid WorkspaceId,
    Guid SpaceId,
    Guid DeletedBy,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(AccountId, WorkspaceId, OccurredAt, DeletedBy);
