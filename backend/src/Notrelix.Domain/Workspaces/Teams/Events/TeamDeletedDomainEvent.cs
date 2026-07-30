namespace Notrelix.Domain.Workspaces.Teams.Events;

[EventName("workspaces.team-deleted")]
public sealed record TeamDeletedDomainEvent(
    Guid AccountId,
    Guid WorkspaceId,
    Guid TeamId,
    Guid DeletedBy,
    TeamStatus Status,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(AccountId, WorkspaceId, OccurredAt);
