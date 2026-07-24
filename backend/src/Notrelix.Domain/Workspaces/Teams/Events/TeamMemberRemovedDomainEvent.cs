namespace Notrelix.Domain.Workspaces.Teams.Events;

[EventName("workspaces.team-member-removed")]
public sealed record TeamMemberRemovedDomainEvent(
    Guid AccountId,
    Guid WorkspaceId,
    Guid TeamId,
    Guid UserId,
    Guid RemovedBy,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(AccountId, WorkspaceId, OccurredAt);
