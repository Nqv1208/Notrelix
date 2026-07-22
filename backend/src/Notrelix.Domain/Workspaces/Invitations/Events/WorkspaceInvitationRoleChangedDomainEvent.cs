namespace Notrelix.Domain.Workspaces.Invitations.Events;

public sealed record WorkspaceInvitationRoleChangedDomainEvent(
    Guid AccountId,
    Guid InvitationId,
    Guid WorkspaceId,
    WorkspaceRole OldRole,
    WorkspaceRole NewRole,
    Guid UpdatedBy,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(AccountId, WorkspaceId, OccurredAt);
