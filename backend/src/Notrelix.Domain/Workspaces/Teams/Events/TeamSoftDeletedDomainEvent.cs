namespace Notrelix.Domain.Workspaces.Teams.Events;

[EventName("workspaces.team-soft-deleted")]
public sealed record TeamSoftDeletedDomainEvent(
    Guid AccountId,
    Guid WorkspaceId,
    Guid TeamId,
    Guid DeletedBy,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(AccountId, WorkspaceId, OccurredAt);
