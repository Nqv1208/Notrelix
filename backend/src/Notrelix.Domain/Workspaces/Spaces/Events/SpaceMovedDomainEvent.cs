namespace Notrelix.Domain.Workspaces.Spaces.Events;

[EventName("workspaces.space-moved")]
public sealed record SpaceMovedDomainEvent(
    Guid AccountId,
    Guid WorkspaceId,
    Guid SpaceId,
    Guid? OldParentSpaceId,
    Guid? NewParentSpaceId,
    string Position,
    Guid MovedBy,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(AccountId, WorkspaceId, OccurredAt);
