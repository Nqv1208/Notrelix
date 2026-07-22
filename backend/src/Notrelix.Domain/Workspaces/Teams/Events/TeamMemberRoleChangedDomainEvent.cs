namespace Notrelix.Domain.Workspaces.Teams.Events;

public sealed record TeamMemberRoleChangedDomainEvent(
    Guid AccountId,
    Guid WorkspaceId,
    Guid TeamId,
    Guid UserId,
    TeamMemberRole OldRole,
    TeamMemberRole NewRole,
    Guid UpdatedBy,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(AccountId, WorkspaceId, OccurredAt);
