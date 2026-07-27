namespace Notrelix.Domain.Workspaces.Invitations.Events;

[EventName("workspaces.workspace-invitation-expired")]
public sealed record WorkspaceInvitationExpiredDomainEvent(
    Guid AccountId,
    Guid InvitationId,
    Guid WorkspaceId,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(AccountId, WorkspaceId, OccurredAt);
