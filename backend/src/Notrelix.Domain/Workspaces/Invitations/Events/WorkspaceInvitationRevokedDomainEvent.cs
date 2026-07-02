namespace Notrelix.Domain.Workspaces.Invitations.Events;

public sealed record WorkspaceInvitationRevokedDomainEvent(
    Guid AccountId,
    Guid InvitationId,
    Guid WorkspaceId,
    Guid RevokedBy,
    DateTimeOffset OccurredAt
) : WorkspaceScopedDomainEvent(AccountId, WorkspaceId, OccurredAt, RevokedBy);
