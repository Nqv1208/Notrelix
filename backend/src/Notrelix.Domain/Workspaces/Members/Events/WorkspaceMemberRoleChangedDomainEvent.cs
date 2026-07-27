namespace Notrelix.Domain.Workspaces.Members.Events;

[EventName("workspaces.workspace-member-role-changed")]
public sealed record WorkspaceMemberRoleChangedDomainEvent(
    Guid AccountId,
    Guid WorkspaceId,
    Guid MemberId,
    Guid UserId,
    WorkspaceRole OldRole,
    WorkspaceRole NewRole,
    Guid ActorId,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(AccountId, WorkspaceId, OccurredAt);
