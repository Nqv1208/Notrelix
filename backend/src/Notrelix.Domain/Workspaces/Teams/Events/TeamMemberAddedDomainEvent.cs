namespace Notrelix.Domain.Workspaces.Teams.Events;

[EventName("workspaces.team-member-added")]
public sealed record TeamMemberAddedDomainEvent(
    Guid AccountId,
    Guid WorkspaceId,
    Guid TeamId,
    Guid UserId,
    TeamMemberRole Role,
    Guid AddedBy,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(AccountId, WorkspaceId, OccurredAt);
