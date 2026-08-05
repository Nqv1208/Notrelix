namespace Notrelix.Domain.Workspaces.Teams.Events;

[EventName("workspaces.team-restored")]
public sealed record TeamRestoredDomainEvent(
    Guid AccountId,
    Guid WorkspaceId,
    Guid TeamId,
    Guid RestoredBy,
    TeamStatus Status,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(AccountId, WorkspaceId, OccurredAt);
