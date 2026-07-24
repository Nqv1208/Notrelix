namespace Notrelix.Domain.Workspaces.Spaces.Events;

[EventName("workspaces.space-created")]
public sealed record SpaceCreatedDomainEvent(
    Guid SpaceId,
    Guid AccountId,
    Guid WorkspaceId,
    string Name,
    Guid CreatedBy,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(AccountId, WorkspaceId, OccurredAt);
