namespace Notrelix.Domain.Workspaces.Invitations.Events;

[EventName("workspaces.workspace-invitation-resent")]
public sealed record WorkspaceInvitationResentDomainEvent(
    Guid AccountId,
    Guid InvitationId,
    Guid WorkspaceId,
    Guid ResentBy,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(AccountId, WorkspaceId, OccurredAt);
