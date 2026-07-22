namespace Notrelix.Domain.Workspaces.Spaces.Events;

public sealed record SpaceDescriptionUpdatedDomainEvent(
    Guid AccountId,
    Guid WorkspaceId,
    Guid SpaceId,
    string? OldDescription,
    string? NewDescription,
    Guid UpdatedBy,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(AccountId, WorkspaceId, OccurredAt);
