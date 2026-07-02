namespace Notrelix.Domain.Workspaces.Invitations.Events;

public sealed record WorkspaceInvitationAcceptedDomainEvent(
    Guid AccountId,
    Guid InvitationId,
    Guid WorkspaceId,
    Guid UserId,
    Guid AcceptedBy,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(AccountId, WorkspaceId, OccurredAt, AcceptedBy);
