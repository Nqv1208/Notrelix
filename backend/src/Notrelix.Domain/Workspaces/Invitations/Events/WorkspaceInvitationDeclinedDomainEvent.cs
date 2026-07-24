namespace Notrelix.Domain.Workspaces.Invitations.Events;

[EventName("workspaces.workspace-invitation-declined")]
public sealed record WorkspaceInvitationDeclinedDomainEvent(
    Guid AccountId,
    Guid InvitationId,
    Guid WorkspaceId,
    Guid DeclinedBy,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(AccountId, WorkspaceId, OccurredAt);
