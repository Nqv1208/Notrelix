namespace Notrelix.Domain.Workspaces.Spaces.Events;

public sealed record SpaceCreatedDomainEvent(
    Guid SpaceId,
    Guid AccountId,
    Guid WorkspaceId,
    string Name,
    Guid CreatedBy,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(AccountId, WorkspaceId, OccurredAt);
