namespace Notrelix.Domain.Workspaces.Spaces.Events;

public sealed record SpaceUnarchivedDomainEvent(
    Guid AccountId,
    Guid WorkspaceId,
    Guid SpaceId,
    Guid UnarchivedBy,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(AccountId, WorkspaceId, OccurredAt, UnarchivedBy);
