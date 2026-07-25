namespace Notrelix.Domain.Workspaces.Invitations.Events;

public sealed record WorkspaceInvitationDeclinedDomainEvent(
    Guid AccountId,
    Guid InvitationId,
    Guid WorkspaceId,
    Guid DeclinedBy,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(AccountId, WorkspaceId, OccurredAt, DeclinedBy);
