namespace Notrelix.Domain.Workspaces.Teams.Events;

[EventName("workspaces.team-archived")]
public sealed record TeamArchivedDomainEvent(
    Guid AccountId,
    Guid WorkspaceId,
    Guid TeamId,
    Guid ArchivedBy,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(AccountId, WorkspaceId, OccurredAt);
