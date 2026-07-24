namespace Notrelix.Domain.Workspaces.Spaces.Events;

[EventName("workspaces.space-renamed")]
public sealed record SpaceRenamedDomainEvent(
    Guid AccountId,
    Guid WorkspaceId,
    Guid SpaceId,
    string OldName,
    string NewName,
    Guid UpdatedBy,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(AccountId, WorkspaceId, OccurredAt);
