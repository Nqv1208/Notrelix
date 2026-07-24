namespace Notrelix.Domain.Workspaces.Invitations.Events;

[EventName("workspaces.workspace-invitation-revoked")]
public sealed record WorkspaceInvitationRevokedDomainEvent(
    Guid AccountId,
    Guid InvitationId,
    Guid WorkspaceId,
    Guid RevokedBy,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(AccountId, WorkspaceId, OccurredAt);
