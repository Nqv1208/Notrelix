namespace Notrelix.Domain.Workspaces.Spaces.Events;

[EventName("workspaces.space-restored")]
public sealed record SpaceRestoredDomainEvent(
    Guid AccountId,
    Guid WorkspaceId,
    Guid SpaceId,
    Guid RestoredBy,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(AccountId, WorkspaceId, OccurredAt);
